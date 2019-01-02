using LiveChatLib.Bilibili.Storage;
using LiveChatLib.Common;
using LiveChatLib.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiveChatLib.Bilibili
{
    public class BilibiliListener : ILiveChatListener
    {
        private DateTime LastSendHeartBeatTime { get; set; }
        private DateTime LastReceiveTime { get; set; }
        private const int HeartBeatDuration = 30000;
        private const int HeartBeatTimeout = 3000;


        public int LiveRoomID { get; set; }
        public FixedSizedQueue<BilibiliMessage> MessageQueue { get; set; }
        public delegate void ProcessMessageHandler(MessageBase message);
        public event ProcessMessageHandler OnProcessMessage;
        public delegate void ProcessUserHandler(User user);
        public event ProcessUserHandler OnProcessUser;
        public delegate void BadCommunicationHandler(WebSocketSharp.WebSocket client);
        public event BadCommunicationHandler OnBadCommunication;
        public ListenerState State { get; private set; }



        public BilibiliListener()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config/bilibili.config");
            State = ListenerState.Disconnected;
            try
            {
                var json = JToken.Parse(File.ReadAllText(path));
                LiveRoomID = json["roomid"].ToObject<int>();
            }
            catch { }
        }

        public async Task KeepMessage(MessageBase message)
        {
            var bmsg = message as BilibiliMessage;
            if (bmsg.MsgType == MessageType.Unknown)
            {
                return;
            }

            var user = Database.PickUserInformation(bmsg.SenderId);
            if (user != null && !string.IsNullOrEmpty(user.Face))
            {
                bmsg.AvatarUrl = user.Face;

                Database.KeepMessage(bmsg);
                OnProcessMessage(message);
            }
            else
            {
                Database.KeepMessage(bmsg);
                OnProcessMessage(message);
                if (string.IsNullOrEmpty(bmsg.AvatarUrl))
                {
                    if (bmsg.Meta.ContainsKey("uid"))
                    {
                        await CacheUser(bmsg.Meta["uid"], u => OnProcessUser(u));
                    }
                }
            }
        }

        public void LoopListening(ref bool StopListenToken)
        {
            // Get Room ID.
            var json = HttpRequests.DownloadString(@"https://api.live.bilibili.com/room/v1/Room/room_init?id=" + LiveRoomID).Result;
            var jobj = JToken.Parse(json);
            var id = jobj["data"]["room_id"].ToObject<int>();

            using (var ws = new WebSocketSharp.WebSocket("wss://broadcastlv.chat.bilibili.com/sub"))
            {
                State = ListenerState.Connecting;
                do
                {
                    if (ws.ReadyState != WebSocketSharp.WebSocketState.Open && ws.ReadyState != WebSocketSharp.WebSocketState.Connecting)
                    {
                        ws.Connect();
                        Thread.Sleep(1000);
                    }
                } while (ws.ReadyState != WebSocketSharp.WebSocketState.Open);

                // Initialize
                var package = PackageBuilder.MakeAuthPackage(0, id);
                ws.OnMessage += Ws_OnMessage;
                var bytes = package.ToByteArray();
                ws.Send(bytes);

                // When the connection is not bad, default action.
                // Reconnect to the live room and resend the auto package.
                OnBadCommunication += c =>
                {
                    c.Close();

                    State = ListenerState.Connecting;
                    do
                    {
                        if (ws.ReadyState == WebSocketSharp.WebSocketState.Closed)
                        {
                            ws.Connect();
                            Thread.Sleep(1000);
                        }
                    } while (ws.ReadyState != WebSocketSharp.WebSocketState.Open);
                    var p = PackageBuilder.MakeAuthPackage(0, id);
                    c.Send(p.ToByteArray());
                };

                // Main loop
                while (!StopListenToken)
                {
                    Thread.Sleep(1000);
                    if (DateTime.Now.Subtract(LastSendHeartBeatTime).TotalMilliseconds >= HeartBeatDuration)
                    {
                        var heartbeat = PackageBuilder.MakeHeatBeat();
                        ws.Send(heartbeat.ToByteArray());
                        LastSendHeartBeatTime = DateTime.Now;
                    }
                    if ((LastReceiveTime.AddMilliseconds(HeartBeatTimeout) < LastSendHeartBeatTime))
                    {
                        State = ListenerState.BadCommunication;
                        OnBadCommunication(ws);
                    }
                }

                ws.Close();
                State = ListenerState.Disconnected;
            }
        }

        private async void Ws_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            LastReceiveTime = DateTime.Now;

            foreach (var m in PackageParser.GetPackages(e.RawData))
            {
                await KeepMessage(new BilibiliMessage(m));
            }
        }

        private void SendHeartBeat(WebSocketSharp.WebSocket ws)
        {
            var package = new Package(MsgType.ClientHeart, new byte[0]);
            var data = package.Body;
            ws.SendAsync(data, null);
        }

        private async Task<User> CacheUser(string uid, Action<User> Callback = null)
        {
            // Pick user if exists and skip caching.
            var inDB = Database.PickUserInformation(Convert.ToInt32(uid));
            if (inDB != null && inDB.LastUpdateTime.Subtract(DateTime.UtcNow).TotalDays < 1)
            {
                return inDB;
            }

            // Call api to get user information.
            var random = new Random();
            var headers = new Dictionary<string, string> {
                {             "User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36" },
                {       "X-Requested-With", "XMLHttpRequest" },
                {                "Referer", "http://space.bilibili.com/"+uid+"?from=search&seid="+random.Next(10000,50000) },
                {                 "Origin", "http://space.bilibili.com" },
                {                   "Host", "space.bilibili.com" },
                { "AlexaToolbar-ALX_NS_PH", "AlexaToolbar/alx-4.0" },
                {        "Accept-Language", "zh-CN,zh;q=0.8,en;q=0.6,ja;q=0.4" },
                {                 "Accept", "application/json, text/javascript, */*; q=0.01" }
            };
            var formData = new Dictionary<string, string>
            {
                {   "_", ((int)DateTime.UtcNow.Subtract(new DateTime(1970,1,1)).TotalMilliseconds).ToString() },
                { "mid", uid }
            };

            // Post and get data from API.
            var result = await HttpRequests.Post(
                    url: "http://space.bilibili.com/ajax/member/GetInfo",
                    formData: formData,
                    headers: headers,
                    encoding: Encoding.UTF8
                );
            var json = JToken.Parse(result);
            var face64 = "";

            // Download avatar.
            if (json["data"]?["face"] != null)
            {
                var facedata = await HttpRequests.DownloadBytes(json["data"]["face"].ToString());
                var bitmap = ImageOptimize.ResizeImage(Image.FromStream(new MemoryStream(facedata)), 64, 64);
                using (var ms = new MemoryStream()) {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    ms.Flush();
                    face64 = Convert.ToBase64String(ms.ToArray());
                }
            }

            // Save the data.
            var user = new User
            {
                BirthDay = json["data"]["birthday"]?.ToString() ?? "保密",
                Face = json["data"]["face"]?.ToString() ?? "",
                FaceBase64 = face64 ?? "",
                Level = json["data"]["level_info"]?["current_level"]?.ToObject<int>() ?? -1,
                Id = json["data"]["mid"]?.ToObject<int>() ?? 0,
                Name = json["data"]["name"]?.ToString() ?? "",
                Sex = json["data"]["sex"]?.ToString() ?? "保密"
            };
            if (user.Id == 0)
            {
                return null;
            }
            Database.SaveUserInformation(user);
            Callback(user);
            return user;
        }
    }
}
