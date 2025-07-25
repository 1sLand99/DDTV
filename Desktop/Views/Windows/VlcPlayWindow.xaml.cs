﻿using Core;
using Core.LiveChat;
using Core.LogModule;
using Core.RuntimeObject;
using Desktop.Models;
using Desktop.Views.Windows.DanMuCanvas.BarrageParameters;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using Microsoft.Extensions.DependencyInjection;
using Notification.Wpf;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;
using static Core.Config;
using Key = System.Windows.Input.Key;
using MenuItem = Wpf.Ui.Controls.MenuItem;


namespace Desktop.Views.Windows
{
    /// <summary>
    /// VlcPlayWindow.xaml 的交互逻辑
    /// </summary>
    public partial class VlcPlayWindow : FluentWindow
    {

        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        /// <summary>
        /// 窗口展示内容数据绑定源
        /// </summary>
        internal VlcPlayModels vlcPlayModels { get; private set; }
        /// <summary>
        /// 当前窗口弹幕开关状态
        /// </summary>
        private bool DanmaSwitch = false;
        /// <summary>
        /// 当前窗口的置顶状态
        /// </summary>
        private bool TopMostSwitch = false;
        /// <summary>
        /// 当前播放窗口所属的房间卡
        /// </summary>
        private RoomCardClass roomCard = new();
        /// <summary>
        /// 弹幕渲染实例
        /// </summary>
        private BarrageConfig barrageConfig;
        /// <summary>
        /// 弹幕发射轨道
        /// </summary>
        public DanMuOrbitInfo[] danMuOrbitInfos = new DanMuOrbitInfo[100];
        /// <summary>
        /// 当前窗口的清晰度
        /// </summary>
        public long CurrentWindowClarity = 10000;
        /// <summary>
        /// 宽高比是否初始化
        /// </summary>
        public bool InitializeAspectRatio = false;
        /// <summary>
        /// 用于跟踪当前是否为全屏状态
        /// </summary>
        public bool isFullScreen = false;
        public class DanMuOrbitInfo
        {
            public string Text { get; set; }
            public int Time { get; set; } = 0;
        }

        public VlcPlayWindow(long uid)
        {
            InitializeComponent();
            vlcPlayModels = new();
            CurrentWindowClarity = Core_RunConfig._DefaultPlayResolution;
            this.DataContext = vlcPlayModels;
            _Room.GetCardForUID(uid, ref roomCard);

            vlcPlayModels.VolumeVisibility = Visibility.Collapsed;
            vlcPlayModels.OnPropertyChanged("VolumeVisibility");


            if (roomCard == null || roomCard.live_status.Value != 1)
            {
                Log.Info(nameof(VlcPlayWindow), $"打开播放器失败，入参uid:{uid},因为{(roomCard == null ? "roomCard为空" : "已下播")}");
                vlcPlayModels.MessageVisibility = Visibility.Visible;
                vlcPlayModels.OnPropertyChanged("MessageVisibility");
                vlcPlayModels.MessageText = "该直播间未开播，播放失败";
                vlcPlayModels.OnPropertyChanged("MessageText");
                return;
            }

            this.Title = $"{roomCard.Name}({roomCard.RoomId}) - {roomCard.Title.Value}";
            Log.Info(nameof(VlcPlayWindow), $"房间号:[{roomCard.RoomId}],打开播放器");

            _libVLC = new LibVLC([$"--network-caching={new Random().Next(5000, 7000)} --no-cert-verification"]);
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

            videoView.MediaPlayer = _mediaPlayer;
            videoView.MediaPlayer.Playing += MediaPlayer_Playing;
            videoView.MediaPlayer.EndReached += MediaPlayer_EndReached;
            videoView.MediaPlayer.Volume = 30;

            Task.Run(() => InitVlcPlay(uid));
            Task.Run(() => SetClarityMenu());
        }
        /// <summary>
        /// 初始化播放器和弹幕渲染Canvas
        /// </summary>
        /// <param name="uid"></param>
        public void InitVlcPlay(long uid)
        {
            PlaySteam(null);
            Dispatcher.Invoke(() =>
            {
                barrageConfig = new BarrageConfig(DanmaCanvas, this.Width);
            });
            if (Core_RunConfig._PlayWindowDanmaSwitch)
            {
                SetDanma();
            }
            Dispatcher.Invoke(() =>
            {
                DanmaCanvas.Opacity = Core_RunConfig._PlayWindowDanMuFontOpacity;
            });
        }

        /// <summary>
        /// 获取和设置分辨率选项
        /// </summary>
        private void SetClarityMenu()
        {
            List<long> DefinitionList = Core.RuntimeObject.Download.Basics.GetOptionalClarity(roomCard.RoomId, "http_hls", "fmp4", "avc");

            Dictionary<long, string> clarityMap = new Dictionary<long, string>
            {
                {30000, "杜比"},
                {20000, "4K"},
                {10000, "原画"},
                {400, "蓝光"},
                {250, "超清"},
                {150, "高清"},
                {80, "流畅"}
            };

            foreach (var clarity in clarityMap)
            {
                if (DefinitionList.Contains(clarity.Key))
                {
                    Dispatcher.Invoke(() =>
                    {
                        MenuItem childMenuItem = new MenuItem
                        {
                            Header = clarity.Value,
                            Tag = clarity.Key
                        };
                        childMenuItem.Click += ModifyResolutionRightClickMenuEvent_Click;
                        SwitchPlaybackClarity_Menu.Items.Add(childMenuItem);
                    });
                }
            }
        }


        private void ModifyResolutionRightClickMenuEvent_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickedMenuItem = sender as MenuItem;

            Dispatcher.Invoke(() =>
            {
                CurrentWindowClarity = (long)clickedMenuItem.Tag; // 获取被点击的菜单项的索引
            });

            vlcPlayModels.LoadingVisibility = Visibility.Visible;
            vlcPlayModels.OnPropertyChanged("LoadingVisibility");

            PlaySteam(null);
        }

        private void MediaPlayer_Playing(object? sender, EventArgs e)
        {
            Task.Run(() =>
            {
                Thread.Sleep(3000);
                vlcPlayModels.LoadingVisibility = Visibility.Collapsed;
                vlcPlayModels.OnPropertyChanged("LoadingVisibility");
                //初始化宽高比
                if (!InitializeAspectRatio)
                {
                    if (_mediaPlayer != null && _mediaPlayer.Media != null && _mediaPlayer.Media.Tracks.Length > 0)
                    {
                        try
                        {
                            var videoWidth = _mediaPlayer.Media.Tracks[0].Data.Video.Width;
                            var videoHeight = _mediaPlayer.Media.Tracks[0].Data.Video.Height;
                            if (videoHeight > videoWidth)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    this.Width = 450;
                                    this.Height = 800;
                                });

                            }
                        }
                        catch (Exception) { }
                        InitializeAspectRatio = true;
                    }


                }
            });
        }

        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            vlcPlayModels.LoadingVisibility = Visibility.Visible;
            vlcPlayModels.OnPropertyChanged("LoadingVisibility");
            PlaySteam(null);
        }

        private async void SetDanma()
        {
            if (DanmaSwitch)
            {
                return;
            }
            DanmaSwitch = true;
            await Task.Run(() =>
            {

                if (roomCard.DownInfo.LiveChatListener == null)
                {
                    roomCard.DownInfo.LiveChatListener = new Core.LiveChat.LiveChatListener(roomCard.RoomId);
                    roomCard.DownInfo.LiveChatListener.Connect();
                }
                if (!roomCard.DownInfo.LiveChatListener.State)
                {
                    roomCard.DownInfo.LiveChatListener.Connect();
                }
                roomCard.DownInfo.LiveChatListener.MessageReceived += LiveChatListener_MessageReceived;
                roomCard.DownInfo.LiveChatListener.Register.Add("VlcPlayWindow");
            });
        }

        private async void CloseDanma()
        {
            DanmaSwitch = false;
            await Task.Run(() =>
            {
                if (roomCard.DownInfo.LiveChatListener != null && roomCard.DownInfo.LiveChatListener.Register.Count > 0)
                {

                    roomCard.DownInfo.LiveChatListener.MessageReceived -= LiveChatListener_MessageReceived;

                    roomCard.DownInfo.LiveChatListener.Register.Remove("VlcPlayWindow");
                    if (roomCard.DownInfo.LiveChatListener.Register.Count == 0)
                    {

                        roomCard.DownInfo.LiveChatListener.Dispose();
                        roomCard.DownInfo.LiveChatListener = null;
                    }

                }
            });
        }

        private void LiveChatListener_MessageReceived(object? sender, Core.LiveChat.MessageEventArgs e)
        {
            LiveChatListener liveChatListener = (LiveChatListener)sender;
            switch (e)
            {
                case DanmuMessageEventArgs Danmu:
                    {
                        string[] BlockWords = Core.Config.Core_RunConfig._BlockBarrageList.Split('|');
                        if (BlockWords.Any(word => !string.IsNullOrEmpty(word) && Danmu.Message.Contains(word)))
                        {
                            return;
                        }
                        AddDanmu(Danmu.Message, false, Danmu.UserId);
                        break;
                    }
            }
        }

        /// <summary>
        /// 播放网络路径直播流
        /// </summary>
        /// <param name="Url"></param>
        public async void PlaySteam(string Url = null)
        {
            Log.Info(nameof(PlaySteam), $"房间号:[{roomCard.RoomId}],播放网络路径直播流");
            await Task.Run(() =>
            {

                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Stop();
                }
                if (_mediaPlayer.Media != null)
                {
                    _mediaPlayer.Media.ClearSlaves();
                    _mediaPlayer.Media = null;
                }


                if (!RoomInfo.GetLiveStatus(roomCard.RoomId))
                {
                    Log.Info(nameof(PlaySteam), $"房间号:[{roomCard.RoomId}]，主播已下播，停止获取流地址");
                    return;
                }
                if (string.IsNullOrEmpty(Url))
                {
                    Url = GeUrl(CurrentWindowClarity);
                }
                try
                {
                    bool completedInTime = false;

                    while (!completedInTime)
                    {
                        CancellationTokenSource cts = new CancellationTokenSource();
                        Task task = Task.Run(() =>
                        {
                            if (_libVLC != null && !string.IsNullOrEmpty(Url))
                            {
                                var media = new Media(_libVLC, Url, FromType.FromLocation);
                                _mediaPlayer.Media = media;
                                _mediaPlayer?.Play();
                            }
                            else
                            {
                                vlcPlayModels.MessageVisibility = Visibility.Visible;
                                vlcPlayModels.OnPropertyChanged("MessageVisibility");
                                vlcPlayModels.MessageText = "直播间已下拨获取地址失败，如需更新请右键刷新";
                                vlcPlayModels.OnPropertyChanged("MessageText");
                                return;
                            }
                        }, cts.Token);

                        if (!task.Wait(TimeSpan.FromSeconds(10)))
                        {
                            cts.Cancel();
                            Log.Warn(nameof(PlaySteam), $"房间号:[{roomCard.RoomId}]，VLC连接源超时，进行重试，源地址[{Url}]");
                            vlcPlayModels.MessageVisibility = Visibility.Visible;
                            vlcPlayModels.OnPropertyChanged("MessageVisibility");
                            vlcPlayModels.MessageText = "连接直播间失败，开始重试";
                            vlcPlayModels.OnPropertyChanged("MessageText");
                        }
                        else
                        {
                            completedInTime = true;
                            vlcPlayModels.MessageVisibility = Visibility.Collapsed;
                            vlcPlayModels.OnPropertyChanged("MessageVisibility");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(nameof(PlaySteam), $"房间号:[{roomCard.RoomId}]，VLC连接源出现意外错误，进行重试，源地址[{Url}]", ex);
                }
            });

        }

        /// <summary>
        /// 获取直播流地址
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public string GeUrl(long Definition)
        {
            string url = "";
            if (roomCard != null && (Core.RuntimeObject.Download.HLS.GetHlsAvcUrl(roomCard, Definition, out url)))
            {
                Log.Info(nameof(GeUrl), $"房间号:[{roomCard.RoomId}]，获取到直播流地址:[{url}]");
                return url;
            }
            return "";
        }

        private void FluentWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Stop();
                }
                if (_mediaPlayer.Media != null)
                {
                    _mediaPlayer.Media.ClearSlaves();
                    _mediaPlayer.Media = null;
                }
                Log.Info(nameof(PlaySteam), $"房间号:[{roomCard.RoomId}],关闭播放器");
            }
            if (DanmaSwitch)
            {
                CloseDanma();
            }
        }

        private DateTime lastClickTime = DateTime.MinValue; // 上次点击的时间

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
            DateTime now = DateTime.Now;
            // 检查是否为双击（两次点击间隔小于系统双击时间）
            if ((now - lastClickTime).TotalMilliseconds <= SystemInformation.DoubleClickTime)
            {
                ToggleFullScreen();
            }
            lastClickTime = now;
        }
        private void ToggleFullScreen()
        {
            if (!isFullScreen)
            {
                // 切换到全屏模式
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                this.ResizeMode = ResizeMode.NoResize;
                isFullScreen = true;
            }
            else
            {
                // 切换回窗口模式
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.WindowState = WindowState.Normal;
                this.ResizeMode = ResizeMode.CanResize;
                isFullScreen = false;
            }
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int v = 0;
            if (_mediaPlayer != null)
            {
                videoView.Dispatcher.Invoke(() => v = _mediaPlayer.Volume);
            }
            if (e.Delta > 0)
            {
                if (v + 5 <= 100)
                {
                    SetVolume(v + 5);
                }
                else
                {
                    SetVolume(100);
                }
            }
            else if (e.Delta < 0)
            {
                if (v - 5 >= 0)
                {
                    SetVolume(v - 5);
                }
                else
                {
                    SetVolume(0);
                }
            }
        }

        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="i"></param>
        private void SetVolume(int i)
        {
            if (videoView != null && _mediaPlayer != null)
            {

                videoView.Dispatcher.Invoke(() =>
                {
                    _mediaPlayer.Volume = i;
                    vlcPlayModels.VolumeVisibility = Visibility.Visible;
                    vlcPlayModels.OnPropertyChanged("VolumeVisibility");
                    Task.Run(() =>
                    {
                        Thread.Sleep(2000);
                        vlcPlayModels.VolumeVisibility = Visibility.Collapsed;
                        vlcPlayModels.OnPropertyChanged("VolumeVisibility");
                    });
                    vlcPlayModels.Volume = i.ToString();
                    vlcPlayModels.OnPropertyChanged("Volume");
                });
            }
        }

        private void ExitWindow_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void AddDanmu(string DanmuText, bool IsSubtitle, long uid = 0)
        {

            Task.Run(() =>
            {
                int Index = 0;
                for (int i = 0; i < danMuOrbitInfos.Length; i++)
                {
                    if (danMuOrbitInfos[i] == null)
                    {
                        danMuOrbitInfos[i] = new();
                    }
                    if (danMuOrbitInfos[i].Time < Init.GetRunTime())
                    {
                        Index = i;
                        break;
                    }
                }
                danMuOrbitInfos[Index].Time = (int)(Init.GetRunTime() + 5);
                //非UI线程调用UI组件
                System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                {
                    //显示弹幕
                    barrageConfig.Barrage_Stroke(new DanMuCanvas.Models.MessageInformation() { content = DanmuText }, Index, IsSubtitle);
                });

            });

        }

        private void FullScreenSwitch()
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void FullScreenSwitch_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            FullScreenSwitch();
        }



        private void FluentWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyStates == Keyboard.GetKeyStates(Key.Up) || e.KeyStates == Keyboard.GetKeyStates(Key.Down))
            {
                int v = 0;
                if (videoView != null && videoView.MediaPlayer != null)
                {
                    videoView.Dispatcher.Invoke(() => v = _mediaPlayer.Volume);
                }
                //音量增加
                if (e.KeyStates == Keyboard.GetKeyStates(Key.Up))
                {
                    if (v + 5 <= 100)
                    {
                        SetVolume(v + 5);
                    }
                    else
                    {
                        SetVolume(100);
                    }
                }
                //音量降低
                else if (e.KeyStates == Keyboard.GetKeyStates(Key.Down))
                {
                    if (v - 5 >= 0)
                    {
                        SetVolume(v - 5);
                    }
                    else
                    {
                        SetVolume(0);
                    }
                }
            }
            //全屏回车
            else if (e.KeyStates == Keyboard.GetKeyStates(Key.Enter))
            {
                FullScreenSwitch();
            }
            //F5刷新
            else if (e.KeyStates == Keyboard.GetKeyStates(Key.F5))
            {
                vlcPlayModels.LoadingVisibility = Visibility.Visible;
                vlcPlayModels.OnPropertyChanged("LoadingVisibility");
                PlaySteam(null);
            }
        }

        private void Send_Danma_Button_Click(object sender, RoutedEventArgs e)
        {
            string T = DanmaOnly_DanmaInput.Text;
            if (string.IsNullOrEmpty(T) && T.Length >40 /*Core.Config.Core_RunConfig._MaximumLengthDanmu*/)
            {
                return;
            }
            Danmu.SendDanmu(roomCard.RoomId.ToString(), T);
            DanmaOnly_DanmaInput.Clear();
        }

        private void DanmaOnly_DanmaInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Windows.Controls.TextBox? textBox = sender as System.Windows.Controls.TextBox;
            int maxlen = Core.Config.Core_RunConfig._MaximumLengthDanmu;
            if (textBox != null && textBox.Text.Length > Core.Config.Core_RunConfig._MaximumLengthDanmu)
            {
                int selectionStart = textBox.SelectionStart;
                textBox.Text = textBox.Text.Substring(0, 20);
                textBox.SelectionStart = selectionStart > 20 ? 20 : selectionStart;
            }
        }

        private void F5_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            vlcPlayModels.LoadingVisibility = Visibility.Visible;
            vlcPlayModels.OnPropertyChanged("LoadingVisibility");
            PlaySteam(null);
        }

        private void MenuItem_Switch_Danma_Send_Click(object sender, RoutedEventArgs e)
        {
            if (DanmaBox.Visibility == Visibility.Collapsed)
            {
                DanmaBox.Visibility = Visibility.Visible;
            }
            else
            {
                DanmaBox.Visibility = Visibility.Collapsed;
            }
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (barrageConfig != null)
                barrageConfig._width = this.Width;
        }

        private void MenuItem_Switch_Danma_Exhibition_Click(object sender, RoutedEventArgs e)
        {
            if (DanmaSwitch)
            {
                SetNotificatom("关闭弹幕显示", $"{roomCard.Name}({roomCard.RoomId})播放窗口的弹幕显示已关闭");
                CloseDanma();
            }
            else
            {
                SetNotificatom("打开弹幕显示", $"{roomCard.Name}({roomCard.RoomId})播放窗口的弹幕显示已打开");
                SetDanma();
            }
        }

        private void SetNotificatom(string Title, string Message = "'")
        {
            Dispatcher.Invoke(() =>
            {
                MainWindow.notificationManager.Show(new NotificationContent
                {
                    Title = Title,
                    Message = Message,
                    Type = NotificationType.Success,
                    Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString("#00CC33")

                });
            });

        }

        private void MenuItem_TopMost_Click(object sender, RoutedEventArgs e)
        {
            if (TopMostSwitch)
            {
                this.Topmost = false;
                TopMostSwitch = false;
                SetNotificatom("撤销窗口置顶", $"{roomCard.Name}({roomCard.RoomId})窗口置顶已关闭");
            }
            else
            {
                this.Topmost = true;
                TopMostSwitch = true;
                SetNotificatom("打开窗口置顶", $"{roomCard.Name}({roomCard.RoomId})窗口置顶已打开");
            }
        }

        private void DanmaOnly_DanmaInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyStates == Keyboard.GetKeyStates(Key.Enter))
            {
                string T = DanmaOnly_DanmaInput.Text;
                if (string.IsNullOrEmpty(T) && T.Length > 40)
                {
                    return;
                }
                Danmu.SendDanmu(roomCard.RoomId.ToString(), T);
                DanmaOnly_DanmaInput.Clear();
            }
        }

        private void MenuItem_OpenLiveUlr_Click(object sender, RoutedEventArgs e)
        {

            var psi = new ProcessStartInfo
            {
                FileName = "https://live.bilibili.com/" + roomCard.RoomId,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void MenuItem_DanmaOnly_Click(object sender, RoutedEventArgs e)
        {
            RoomCardClass roomCardClass = new();
            _Room.GetCardForUID(roomCard.UID, ref roomCardClass);
            Windows.DanmaOnlyWindow danmaOnlyWindow = new(roomCardClass);
            danmaOnlyWindow.Show();
        }

        private void PasteStreamAddress_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer?.Media == null || string.IsNullOrEmpty(_mediaPlayer.Media.Mrl))
            {
                Log.Info("VlcPlayWindow", "流地址复制失败：当前无有效流地址（Media未初始化或Mrl为空）");
                return;
            }

            string streamAddress = _mediaPlayer.Media.Mrl;

            try
            {
                System.Windows.Clipboard.SetText(streamAddress);
                Log.Info("VlcPlayWindow", $"流地址已复制到剪贴板：{streamAddress}");
            }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException || ex is System.Threading.ThreadStateException)
            {
                // 特定处理剪贴板相关的异常（如COM异常、线程状态异常）
                Log.Warn("VlcPlayWindow", $"流地址复制到剪贴板失败（剪贴板访问错误）", ex, false);
            }
            catch (Exception ex)
            {
                Log.Warn("VlcPlayWindow", $"流地址复制到剪贴板失败（未知错误）", ex, false);
            }
        }
    }
}
