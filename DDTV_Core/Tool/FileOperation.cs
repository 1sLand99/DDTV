﻿using DDTV_Core.SystemAssembly.BilibiliModule.Rooms;
using DDTV_Core.SystemAssembly.ConfigModule;
using DDTV_Core.SystemAssembly.Log;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static DDTV_Core.SystemAssembly.DataCacheModule.DataCacheClass;

namespace DDTV_Core.Tool
{
    public class FileOperation
    {
        private static DelEvent delEvent = new DelEvent();

        /// <summary>
        /// 判断网络路径的文件是否存在
        /// </summary>
        /// <param name="Url"></param>
        /// <returns></returns>
        public static bool IsExistsNetFile(string Url)
        {
            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.CreateDefault(new Uri(Url));
                httpWebRequest.Accept = "*/*";
                httpWebRequest.UserAgent = SystemAssembly.NetworkRequestModule.NetClass.UA();
                httpWebRequest.Method = "GET";
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                httpWebRequest.Referer = "https://www.bilibili.com/";
                if (!string.IsNullOrEmpty(BilibiliUserConfig.account.cookie))
                {
                    httpWebRequest.Headers.Add("Cookie", BilibiliUserConfig.account.cookie);
                }
                httpWebRequest.Timeout = 5000;
                //返回响应状态是否是成功比较的布尔值
                if (((HttpWebResponse)httpWebRequest.GetResponse()).StatusCode == HttpStatusCode.OK)
                {

                }
                return true;
            }
            catch (Exception e)
            {
                Log.AddLog(nameof(FileOperation), LogClass.LogType.Warn, Url+"   " + e.Message,false,null,false);
                //Log.AddLog(nameof(FileOperation),LogClass.LogType.Warn, "请求的网络路径地址:\n" + Url, false, null, false);
                return false;
            }
        }
        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="File"></param>
        public static void Del(string File)
        {
            delEvent.AddFile(File);
        }
        public static void Del(List<string> File)
        {
            foreach (var item in File)
            {
                delEvent.AddFile(item);
            }
        }
        /// <summary>
        /// 检查字符串是否符合文件路径标准
        /// </summary>
        /// <param name="Text"></param>
        /// <returns>返回清除不符合要求的字符后的字符串</returns>
        public static string CheckFilenames(string Text)
        {
            Text = Text.Replace(" ", string.Empty).Replace("/", string.Empty).Replace("\\", string.Empty).Replace("\"", string.Empty).Replace(":", string.Empty).Replace("*", string.Empty).Replace("?", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Replace("|", string.Empty).Replace("#", string.Empty).Replace("&", string.Empty).Replace("=", string.Empty).Replace("%", string.Empty).Replace("\0", string.Empty);
            StringBuilder rBuilder = new StringBuilder(Text);
            foreach (char rInvalidChar in Path.GetInvalidPathChars())
                rBuilder = rBuilder.Replace(rInvalidChar.ToString(), string.Empty);
            Text = rBuilder.ToString();
            return Text;
        }
        /// <summary>
        /// 替换关键字(用于替换预设的关键字如{roomid},{name}之类的)
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        public static string ReplaceKeyword(long uid, string Text)
        {
            Rooms.RoomInfo.TryGetValue(uid, out RoomInfoClass.RoomInfo roomInfo);

            if (roomInfo.DownloadedFileInfo.FlvFile != null)
                Text = Text.Replace("{FlvFile}", roomInfo.DownloadedFileInfo.FlvFile.FullName);

            if (roomInfo.DownloadedFileInfo.Mp4File != null)
                Text = Text.Replace("{MP4File}", roomInfo.DownloadedFileInfo.Mp4File.FullName);

            if (roomInfo.DownloadedFileInfo.DanMuFile != null)
                Text = Text.Replace("{DanMuFile}", roomInfo.DownloadedFileInfo.DanMuFile.FullName);

            if (roomInfo.DownloadedFileInfo.SCFile != null)
                Text = Text.Replace("{SCFile}", roomInfo.DownloadedFileInfo.SCFile.FullName);

            if (roomInfo.DownloadedFileInfo.GuardFile != null)
                Text = Text.Replace("{GuardFile}", roomInfo.DownloadedFileInfo.GuardFile.FullName);

            if (roomInfo.DownloadedFileInfo.GiftFile != null)
                Text = Text.Replace("{GiftFile}", roomInfo.DownloadedFileInfo.GiftFile.FullName);

            Text = Text
                .Replace("{ROOMID}", Rooms.GetValue(uid, CacheType.room_id))
                .Replace("{NAME}", Rooms.GetValue(uid, CacheType.uname))
                .Replace("{DATE}", DateTime.Now.ToString("yyMMdd"))
                .Replace("{TIME}", DateTime.Now.ToString("HH-mm-ss"))
                .Replace("{TITLE}", Rooms.GetValue(uid, CacheType.title))
                .Replace("{R}", new Random().Next(1000, 9999).ToString());
            return Text;
        }
        /// <summary>
        /// 在指定路径中创建所有目录
        /// </summary>
        /// <param name="Path">指定的路径</param>
        /// <returns></returns>
        public static string CreateAll(string Path)
        {
            Directory.CreateDirectory(Path);
            return Path;
        }
        /// <summary>
        /// 通过盘符获取剩余空间
        /// </summary>
        /// <param name="str_HardDiskName">盘符 如 C D E</param>
        /// <param name="Type">1为总空间，2为剩余空间</param>
        /// <returns></returns>
        public static long GetHardDiskSpace(string str_HardDiskName,int Type)
        {
            long totalSize = 0;
            str_HardDiskName = str_HardDiskName + ":\\";
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                if (drive.Name == str_HardDiskName)
                {
                    switch (Type)
                    {
                        case 1:
                            return drive.TotalSize;
                        case 2:
                            return drive.TotalFreeSpace;
                    }
                }
            }
            return totalSize;
        }

        /// <summary>
        /// 文件删除服务
        /// </summary>
        private class DelEvent
        {
            private List<string> DelFilelist = new();
            private bool IsDelEnable = false;
            internal void AddFile(string File)
            {
                DelFilelist.Add(File);
                if (!IsDelEnable)
                {
                    IsDelEnable = true;
                    Del();
                }
            }
            private void Del()
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            for (int i = DelFilelist.Count - 1; i >=0; i--)
                            {
                                if (File.Exists(DelFilelist[i]))
                                {
                                    try
                                    {
                                        File.Delete(DelFilelist[i]);
                                        DelFilelist.RemoveAt(i);
                                    }
                                    catch (Exception) { }
                                }
                                else
                                {
                                    try
                                    {
                                        DelFilelist.RemoveAt(i);
                                    }
                                    catch (Exception) { }
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                        Thread.Sleep(2000);
                    }
                });
            }
        }
    }
    public class DownloadList
    {
        private static ArrayList dirs = new();
        public static ArrayList GetRecFileList()
        {
            dirs.Add(SystemAssembly.DownloadModule.Download.DownloadPath);
            GetDirs(SystemAssembly.DownloadModule.Download.DownloadPath);
            object[] allDir = dirs.ToArray();
            ArrayList list = new ArrayList();
            foreach (object o in allDir)
            {
                list.AddRange(GetFileName(o.ToString()));
            }
            dirs = new();
            return list;
        } 
        private static void GetDirs(string dirPath)
        {
            if (Directory.GetDirectories(dirPath).Length > 0)
            {
                foreach (string path in Directory.GetDirectories(dirPath))
                {
                    dirs.Add(path);
                    GetDirs(path);
                }
            }
        }
        private static ArrayList GetFileName(string dirPath)
        {
            ArrayList list = new ArrayList();
            if (Directory.Exists(dirPath))
            {
                int Conut = Directory.GetFiles(dirPath).Length;
                string[] _ = new string[Conut];
                for (int i = 0 ; i < Conut ; i++)
                {
                    _[i] = Directory.GetFiles(dirPath)[i].Replace("\\", "/");
                }

                list.AddRange(_);
            }
            return list;
        }
    }

}
