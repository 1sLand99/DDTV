﻿using Core;
using Core.LogModule;
using Core.RuntimeObject;
using System.Windows;
using Wpf.Ui.Controls;

namespace Desktop.Views.Windows
{
    /// <summary>
    /// PlayWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WebPlayWindow : FluentWindow
    {
        /// <summary>
        /// 当前窗口的置顶状态
        /// </summary>
        private bool TopMostSwitch = false;

        long _room_id = 0;
        long _uid = 0;
        string _nickname = string.Empty;
        //RoomCardClass _roomCard;
        public WebPlayWindow(long room_id, long uid)
        {
            _room_id = room_id;
            _uid = uid;
            InitializeComponent();
            Task.Run(() =>
            {
                _nickname = RoomInfo.GetNickname(_uid);
                Dispatcher.Invoke(() =>
                {
                    this.Title = RoomInfo.GetTitle(_uid);
                    UI_TitleBar.Title = $"{_nickname}({_room_id}) - {this.Title}【WEB兼容模式】(可能由于该直播间只有FLV流或者网络质量不佳)";
                });
            });

            if (Core.Config.Core_RunConfig._CompatibilityModeDefaultsToOpeningPopupWindow)
            {

                RoomCardClass roomCardClass = new();
                _Room.GetCardForUID(_uid, ref roomCardClass);
                if (roomCardClass != null && roomCardClass.RoomId != 0)
                {
                    Windows.DanmaOnlyWindow danmaOnlyWindow = new(roomCardClass);
                    danmaOnlyWindow.Show();
                }
            }

            Log.Info(nameof(WebPlayWindow), $"房间号:[{room_id}],打开播放器");
            //是否置顶
            if (Config.Core_RunConfig._CompatibilityWindowTop)
            {
                this.Topmost = true;
                TopMostSwitch = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.WV2.Dispose();
                this.WV2 = null;
            });
            Log.Info(nameof(WebPlayWindow), $"房间号:[{_room_id}],关闭播放器");
        }

        private async void WV2_Loaded(object sender, RoutedEventArgs e)
        {
            await WV2.EnsureCoreWebView2Async(null);
            try
            {
                string C = string.Empty;
                if (Core.Config.Core_RunConfig._DesktopRemoteServer || Core.Config.Core_RunConfig._LocalHTTPMode)
                {
                    C = NetWork.Get.GetBody<string>($"{Config.Core_RunConfig._DesktopIP}:{Config.Core_RunConfig._DesktopPort}/api/system/get_c").Replace(" ", "");
                    if (string.IsNullOrEmpty(C))
                    {
                        C = NetWork.Get.GetBody<string>($"{Config.Core_RunConfig._DesktopIP}:{Config.Core_RunConfig._DesktopPort}/api/system/get_c").Replace(" ", "");
                    }
                }
                else
                {
                    C = Core.RuntimeObject.Account.AccountInformation.strCookies;
                }

                
                foreach (var item in C.Split(';'))
                {
                    if (item != null && item.Split('=').Length == 2)
                    {
                        string name = item.Split('=')[0].TrimStart().TrimEnd();
                        string value = item.Split('=')[1].TrimStart().TrimEnd();
                        string D = ".bilibili.com";

                        var cookie = WV2.CoreWebView2.CookieManager.CreateCookie(name, value, D, "/");
                        WV2.CoreWebView2.CookieManager.AddOrUpdateCookie(cookie);
                    }
                }
                string uc = test.UC;
                WV2.CoreWebView2.Navigate($"{uc}{_room_id}&send=0&recommend=0&fullscreen=0");
            }
             catch (Exception EX)
            {
                Log.Error(nameof(WebPlayWindow), $"房间号:[{_room_id}],打开错误", EX, true);
            }
        }
        internal class test
        {
            public static string UC
            {
                get
                {
                    string t = string.Empty;
                    t += (char)104;
                    t += (char)116;
                    t += (char)116;
                    t += (char)112;
                    t += (char)115;
                    t += (char)58;
                    t += (char)47;
                    t += (char)47;
                    t += (char)119;
                    t += (char)119;
                    t += (char)119;
                    t += (char)46;
                    t += (char)98;
                    t += (char)105;
                    t += (char)108;
                    t += (char)105;
                    t += (char)98;
                    t += (char)105;
                    t += (char)108;
                    t += (char)105;
                    t += (char)46;
                    t += (char)99;
                    t += (char)111;
                    t += (char)109;
                    t += (char)47;
                    t += (char)98;
                    t += (char)108;
                    t += (char)97;
                    t += (char)99;
                    t += (char)107;
                    t += (char)98;
                    t += (char)111;
                    t += (char)97;
                    t += (char)114;
                    t += (char)100;
                    t += (char)47;
                    t += (char)108;
                    t += (char)105;
                    t += (char)118;
                    t += (char)101;
                    t += (char)47;
                    t += (char)108;
                    t += (char)105;
                    t += (char)118;
                    t += (char)101;
                    t += (char)45;
                    t += (char)97;
                    t += (char)99;
                    t += (char)116;
                    t += (char)105;
                    t += (char)118;
                    t += (char)105;
                    t += (char)116;
                    t += (char)121;
                    t += (char)45;
                    t += (char)112;
                    t += (char)108;
                    t += (char)97;
                    t += (char)121;
                    t += (char)101;
                    t += (char)114;
                    t += (char)46;
                    t += (char)104;
                    t += (char)116;
                    t += (char)109;
                    t += (char)108;
                    t += (char)63;
                    t += (char)99;
                    t += (char)105;
                    t += (char)100;
                    t += (char)61;
                    return t;
                }
            }
        }

        private void MenuItem_TopMost_Click(object sender, RoutedEventArgs e)
        {
            if (TopMostSwitch)
            {
                this.Topmost = false;
                TopMostSwitch = false;
            }
            else
            {
                this.Topmost = true;
                TopMostSwitch = true;
            }
        }
    }
}