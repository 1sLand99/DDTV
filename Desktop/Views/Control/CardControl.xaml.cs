﻿using Core;
using Core.LogModule;
using Core.RuntimeObject;
using Desktop.Models;
using Desktop.Views.Windows;
using Microsoft.VisualBasic.Logging;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Log = Core.LogModule.Log;

namespace Desktop.Views.Control
{
    /// <summary>
    /// CardControl.xaml 的交互逻辑
    /// </summary>
    public partial class CardControl : UserControl
    {
        public CardControl()
        {
            InitializeComponent();
        }
        private Models.DataCard GetDataCard(object sender)
        {
            var menuItem = (System.Windows.Controls.MenuItem)sender;
            var contextMenu = (ContextMenu)menuItem.Parent;
            var grid = (Grid)contextMenu.PlacementTarget;
            if (grid != null)
            {
                try
                {
                    if(grid.DataContext.GetType()!= typeof(Models.DataCard))
                    {
                       Log.Warn(nameof(GetDataCard),"因为快速操作，导致UI关键对象跟踪失败");
                    }
                    Models.DataCard dataContext = (Models.DataCard)grid.DataContext;
                     return dataContext;
                }
                catch (Exception e)
                {
                    Log.Warn(nameof(GetDataCard),"获取房间卡片快照失败:1",e,true);
                    return new Models.DataCard();
                }
               
            }
            else
            {
                Log.Warn(nameof(GetDataCard),"获取房间卡片快照失败:2");
                //如果触发了这里，说明UI有BUG，需要修复
                return new Models.DataCard();
            }
            
        }

        private void MenuItem_PlayWindow_Click(object sender, RoutedEventArgs e)
        {
            Models.DataCard dataCard = GetDataCard(sender);
            Task.Run(() =>
            {
                if (IsThereHLVPresent(dataCard.Uid))
                {
                    Dispatcher.Invoke(() =>
                    {
                        Windows.VlcPlayWindow vlcPlayWindow = new Windows.VlcPlayWindow(dataCard.Uid);
                        vlcPlayWindow.Show();
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        Windows.WebPlayWindow WebPlayWindow = new Windows.WebPlayWindow(dataCard.Room_Id,dataCard.Uid);
                        WebPlayWindow.Show();
                    });
                }
            });
        }

        /// <summary>
        /// 是否有HLS流
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool IsThereHLVPresent(long uid)
        {
            RoomCardClass roomCard = new();
            _Room.GetCardForUID(uid, ref roomCard);
            string url = "";
            if (roomCard != null && (Core.RuntimeObject.Download.HLS.GetHlsAvcUrl(roomCard, Core.Config.Core_RunConfig._DefaultPlayResolution, out url)) && !string.IsNullOrEmpty(url))
            {
                return true;
            }
            return false;
        }


        private void Border_DoubleClickToOpenPlaybackWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var border = (Border)sender;
                var grid = (Grid)border.Parent;

                Models.DataCard dataCard = (Models.DataCard)grid.DataContext;
                if (IsThereHLVPresent(dataCard.Uid))
                {
                    Windows.VlcPlayWindow vlcPlayWindow = new Windows.VlcPlayWindow(dataCard.Uid);
                    vlcPlayWindow.Show();
                }
                else
                {
                    Windows.WebPlayWindow WebPlayWindow = new Windows.WebPlayWindow(dataCard.Room_Id,dataCard.Uid);
                    WebPlayWindow.Show();
                }

            }
        }


        private void MenuItem_ModifyRoom_AutoRec_Click(object sender, RoutedEventArgs e)
        {
            Models.DataCard dataCard = GetDataCard(sender);
            DataSource.RetrieveData.RoomInfo.ModifyRoomSettings(dataCard.Uid, !dataCard.IsRec, dataCard.IsDanmu, dataCard.IsRemind);
        }

        private void MenuItem_ModifyRoom_Danmu_Click(object sender, RoutedEventArgs e)
        {
            Models.DataCard dataCard = GetDataCard(sender);
            DataSource.RetrieveData.RoomInfo.ModifyRoomSettings(dataCard.Uid, dataCard.IsRec, !dataCard.IsDanmu, dataCard.IsRemind);
        }

        private void MenuItem_ModifyRoom_Remind_Click(object sender, RoutedEventArgs e)
        {
            Models.DataCard dataCard = GetDataCard(sender);
            DataSource.RetrieveData.RoomInfo.ModifyRoomSettings(dataCard.Uid, dataCard.IsRec, dataCard.IsDanmu, !dataCard.IsRemind);
        }

        private void DelRoom_Click(object sender, RoutedEventArgs e)
        {
            Models.DataCard dataCard = GetDataCard(sender);
            Dictionary<string, string> dic = new Dictionary<string, string>
            {
                {"uids", dataCard.Uid.ToString() }
            };
            Task.Run(() =>
            {
                List<(long key, bool State, string Message)> State = new();

                if (Core.Config.Core_RunConfig._DesktopRemoteServer || Core.Config.Core_RunConfig._LocalHTTPMode)
                {
                    State = NetWork.Post.PostBody<List<(long key, bool State, string Message)>>($"{Config.Core_RunConfig._DesktopIP}:{Config.Core_RunConfig._DesktopPort}/api/set_rooms/batch_delete_rooms", dic).Result;
                }
                else
                {
                    State = Core.RuntimeObject._Room.BatchDeleteRooms(dataCard.Uid.ToString());
                }


                if (State == null)
                {
                    Log.Warn(nameof(DelRoom_Click), "调用Core的API[batch_delete_rooms]删除房间失败，返回的对象为Null，详情请查看Core日志", null, true);
                    Dispatcher.Invoke(() =>
                    {
                        MainWindow.SnackbarService.Show("删除房间失败", $"操作{dataCard.Nickname}({dataCard.Room_Id})时调用Core的API[batch_delete_rooms]删除房间失败", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle20), TimeSpan.FromSeconds(3));
                    });
                    return;
                }
                Dispatcher.Invoke(() =>
                {
                    MainWindow.SnackbarService.Show("删除房间成功", $"{dataCard.Nickname}({dataCard.Room_Id})已从房间配置中删除", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark20), TimeSpan.FromSeconds(3));
                });

            });

        }

        private void Cancel_Task_Click(object sender, RoutedEventArgs e)
        {
            Models.DataCard dataCard = GetDataCard(sender);
            Dictionary<string, string> dic = new Dictionary<string, string>
            {
                {"uid", dataCard.Uid.ToString() }
            };
            Task.Run(() =>
            {
                bool State = false;

                if (Core.Config.Core_RunConfig._DesktopRemoteServer || Core.Config.Core_RunConfig._LocalHTTPMode)
                {
                   State = NetWork.Post.PostBody<bool>($"{Config.Core_RunConfig._DesktopIP}:{Config.Core_RunConfig._DesktopPort}/api/rec_task/cancel_task", dic).Result;
                }
                else
                {
                    State = Core.RuntimeObject._Room.CancelTask(dataCard.Uid).State;
                }


                if (State == false)
                {
                    Log.Warn(nameof(DelRoom_Click), "调用Core的API[cancel_task]取消录制任务失败，详情请查看Core日志", null, true);
                    Dispatcher.Invoke(() =>
                    {
                        MainWindow.SnackbarService.Show("取消录制失败", $"操作{dataCard.Nickname}({dataCard.Room_Id})时调用Core的API[cancel_task]取消录制任务失败", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle20), TimeSpan.FromSeconds(3));
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        MainWindow.SnackbarService.Show("取消录制成功", $"已取消{dataCard.Nickname}({dataCard.Room_Id})的录制任务", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark20), TimeSpan.FromSeconds(3));
                    });
                }
            });
        }

        private void MenuItem_DanmaOnly_Click(object sender, RoutedEventArgs e)
        {
            Models.DataCard dataCard = GetDataCard(sender);
            RoomCardClass roomCardClass = new();
            _Room.GetCardForUID(dataCard.Uid, ref roomCardClass);
            Windows.DanmaOnlyWindow danmaOnlyWindow = new(roomCardClass);
            danmaOnlyWindow.Show();
        }

        private void MenuItem_OpenLiveUlr_Click(object sender, RoutedEventArgs e)
        {
            Models.DataCard dataCard = GetDataCard(sender);
            var psi = new ProcessStartInfo
            {
                FileName = "https://live.bilibili.com/" + dataCard.Room_Id,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void Snapshot_Task_Click(object sender, RoutedEventArgs e)
        {
            Models.DataCard dataCard = GetDataCard(sender);
            Dictionary<string, string> dic = new Dictionary<string, string>
            {
                {"uid", dataCard.Uid.ToString() }
            };
            Task.Run(() =>
            {
                (bool state, string message) message = new();

                if (Core.Config.Core_RunConfig._DesktopRemoteServer || Core.Config.Core_RunConfig._LocalHTTPMode)
                {
                    message = NetWork.Post.PostBody<(bool state, string message)>($"{Config.Core_RunConfig._DesktopIP}:{Config.Core_RunConfig._DesktopPort}/api/rec_task/generate_snapshot", dic, new TimeSpan(0, 1, 0)).Result;
                }
                else
                {
                    message = Core.RuntimeObject.Download.Snapshot.CreateRecordingSnapshot(dataCard.Uid);
                }

                if (!message.state)
                {
                    Log.Info(nameof(Snapshot_Task_Click), $"生成直播间录制快照失败，原因:{message.message}");
                    Dispatcher.Invoke(() =>
                    {
                        MainWindow.SnackbarService.Show("快照失败", $"生成直播间录制快照失败，原因:{message.message}", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle20), TimeSpan.FromSeconds(5));
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        MainWindow.SnackbarService.Show("快照完成", $"生成直播间录制快照完成，已输出到DDTV临时文件夹中（{message.message}）", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark20), TimeSpan.FromSeconds(10));
                    });
                }
            });
        }

        private void MenuItem_Compatible_PlayWindow_Click(object sender, RoutedEventArgs e)
        {
            Models.DataCard dataCard = GetDataCard(sender);
            Dispatcher.Invoke(() =>
            {
                Windows.WebPlayWindow WebPlayWindow = new Windows.WebPlayWindow(dataCard.Room_Id,dataCard.Uid);
                WebPlayWindow.Show();
            });
        }
    }
}
