using DDTV_Core.SystemAssembly.BilibiliModule.Rooms;
using DDTV_Core.SystemAssembly.ConfigModule;
using DDTV_Core.SystemAssembly.DownloadModule;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace DDTV_WEB_Server.Controllers
{
    public class Room_Info : ProcessingControllerBase.ApiControllerBase
    {
        [HttpPost(Name = "Room_Info")]
        public string Post([FromForm] string cmd)
        {
            //Response.ContentType = "application/json";
            Dictionary<long, RoomInfoClass.RoomInfo> keyValuePairs = Rooms.RoomInfo;
            foreach (var pair in keyValuePairs)
            {
                pair.Value.roomWebSocket = null;
                pair.Value.DownloadedLog = null;
                pair.Value.DownloadingList = null;
                pair.Value.DanmuFile = null;
            }
            return MessageBase.Success(nameof(Room_Info), keyValuePairs);
        }
    }
    public class Room_Add : ProcessingControllerBase.ApiControllerBase
    {
        [HttpPost(Name = "Room_Add")]
        public string Post([FromForm] long uid, [FromForm] string cmd)
        {
            int RoomId = int.Parse(Rooms.GetValue(uid, DDTV_Core.SystemAssembly.DataCacheModule.DataCacheClass.CacheType.room_id));
            DDTV_Core.SystemAssembly.ConfigModule.RoomConfig.AddRoom(uid, RoomId, "", true);
            return MessageBase.Success(nameof(Room_Add), "������");
        }
    }
    public class Room_Del : ProcessingControllerBase.ApiControllerBase
    {
        [HttpPost(Name = "Room_Del")]
        public string Post([FromForm] long uid, [FromForm] string cmd)
        {
            if(RoomConfig.DeleteRoom(uid))
            {
                return MessageBase.Success(nameof(Room_Del), "ɾ�����");
            }
            else
            {
                return MessageBase.Success(nameof(Room_Del), "�÷��䲻���ڻ����δ֪����ɾ��ʧ��", "�÷��䲻���ڻ����δ֪����ɾ��ʧ��",MessageBase.code.APIAuthenticationFailed);
            }         
        }
    }
    public class Room_AutoRec : ProcessingControllerBase.ApiControllerBase
    {
        [HttpPost(Name = "Room_AutoRec")]
        public string Post([FromForm] long uid, [FromForm] bool IsAutoRec, [FromForm] string cmd)
        {
            RoomConfigClass.RoomCard roomCard = new RoomConfigClass.RoomCard()
            {
                UID = uid,
                IsAutoRec = IsAutoRec
            };
            if (RoomConfig.ReviseRoom(roomCard, false, 2))
            {
               
                if (IsAutoRec && Rooms.GetValue(uid, DDTV_Core.SystemAssembly.DataCacheModule.DataCacheClass.CacheType.live_status) == "1")
                {
                    Download.AddDownloadTaskd(uid, true);
                }
                return MessageBase.Success(nameof(Room_AutoRec), "��" + (IsAutoRec ? "��" : "�ر�") + $"UIDΪ{uid}�ķ��俪���Զ�¼��");
            }
            else
            {
                return MessageBase.Success(nameof(Room_AutoRec), $"�޸�UIDΪ{uid}�Ŀ����Զ�¼�Ƴ������⣬�޸�ʧ��", $"�޸�UIDΪ{uid}�Ŀ����Զ�¼�Ƴ������⣬�޸�ʧ��",MessageBase.code.AutoRecRoomFailed);
            }
        }
    }
    public class Room_DanmuRec : ProcessingControllerBase.ApiControllerBase
    {
        [HttpPost(Name = "Room_DanmuRec")]
        public string Post([FromForm] long uid, [FromForm] bool IsRecDanmu, [FromForm] string cmd)
        {
            RoomConfigClass.RoomCard roomCard = new RoomConfigClass.RoomCard()
            {
                UID = uid,
                IsRecDanmu = IsRecDanmu
            };
            if (RoomConfig.ReviseRoom(roomCard, false, 6))
            {
                return MessageBase.Success(nameof(Room_DanmuRec), "��" + (IsRecDanmu ? "��" : "�ر�") + $"UIDΪ{uid}�ĵ�Ļ¼��");
            }
            else
            {
                return MessageBase.Success(nameof(Room_DanmuRec), $"�޸�UIDΪ{uid}�ĵ�Ļ¼�Ƴ������⣬�޸�ʧ��", $"�޸�UIDΪ{uid}�ĵ�Ļ¼�Ƴ������⣬�޸�ʧ��", MessageBase.code.DanmuRecRoomFailed);
            }
        }
    }
}