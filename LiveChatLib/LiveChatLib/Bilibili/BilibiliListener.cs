using LiveChatLib.Bilibili.Storage;
using LiveChatLib.Common;
using LiveChatLib.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const int HeartBeatDuration = 28000;
        private const int HeartBeatTimeout = 5000;
        private bool waitBack = false;


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
            Trace.TraceInformation("BilibiliListener: Create bilibili comments listener.");
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config/bilibili.config");
            State = ListenerState.Disconnected;
            try
            {
                var json = JToken.Parse(File.ReadAllText(path));
                LiveRoomID = json["roomid"].ToObject<int>();
            }
            catch(Exception exc) {
                Trace.TraceError("BilibiliListener: Failed to load config from " + path);
                Trace.TraceError(exc.Message);
                Trace.TraceError(exc.StackTrace);
            }
        }

        public async Task KeepMessage(MessageBase message)
        {
            var bmsg = message as BilibiliMessage;
            var traceLog = true;

            if (bmsg.Meta.ContainsKey("renqi"))
            {
                traceLog = false;
            }

            var user = Database.PickUserInformation(bmsg.SenderId);
            if (user != null && !string.IsNullOrEmpty(user.Face))
            {
                Trace.TraceInformation("BilibiliListener: Get user face from database.");
                bmsg.AvatarUrl = user.Face;
                bmsg.AvatarBase64 = user.FaceBase64;

                Database.KeepMessage(bmsg);

                if (traceLog)
                {
                    Trace.TraceInformation("BilibiliListener: Received Message.");
                    Trace.TraceInformation("BilibiliListener: Message: " + JsonConvert.SerializeObject(bmsg));
                }
                OnProcessMessage(bmsg);
            }
            else
            {
                if (traceLog)
                {
                    Trace.TraceInformation("BilibiliListener: Received Message.");
                    Trace.TraceInformation("BilibiliListener: Message: " + JsonConvert.SerializeObject(bmsg));
                }
                Database.KeepMessage(bmsg);
                OnProcessMessage(bmsg);
                if (string.IsNullOrEmpty(bmsg.AvatarBase64))
                {
                    if (bmsg.Meta.ContainsKey("uid"))
                    {
                        try
                        {
                            await CacheUser(bmsg.Meta["uid"], u => OnProcessUser(u));
                        }
                        catch (Exception exc) {
                            Trace.TraceError("無法獲取用戶資訊 UID: " + bmsg.Meta["uid"]);
                            Trace.TraceError(exc.Message);
                            Trace.TraceError(exc.StackTrace);
                        }
                    }
                }
            }
        }

        public void LoopListening(ref bool StopListenToken)
        {
            // Get Room ID.
            var txt = HttpRequests.DownloadString(@"https://api.live.bilibili.com/room/v1/Room/room_init?id=" + LiveRoomID).Result;
            var jobj = JToken.Parse(txt);
            var id = jobj["data"]["room_id"].ToObject<int>();

            // Get Room token.
            txt = HttpRequests.DownloadString(@"https://api.live.bilibili.com/room/v1/Danmu/getConf?room_id=" + LiveRoomID).Result;
            jobj = JToken.Parse(txt);
            var token = jobj["data"]["token"].ToString();

            using (var ws = new WebSocketSharp.WebSocket("wss://broadcastlv.chat.bilibili.com/sub"))
            {
                State = ListenerState.Connecting;
                ws.Connect();

                // Initialize
                var package = PackageBuilder.MakeAuthPackage(0, id, token);
                Trace.TraceWarning($"BilibiliListener: Connecting:{id}...");
                ws.OnMessage += Ws_OnMessage;
                var bytes = package.ToByteArray();
                ws.Send(bytes);

                // When the connection is not bad, default action.
                // Reconnect to the live room and resend the auto package.
                OnBadCommunication += c =>
                {
                    c.Close();
                    Thread.Sleep(1000);
                    c.Connect();
                    var counter = 3;
                    while (counter-- > 0)
                    {
                        if (c.ReadyState == WebSocketSharp.WebSocketState.Open)
                        {
                            var p = PackageBuilder.MakeAuthPackage(0, id, token);
                            c.Send(p.ToByteArray());
                            LastSendHeartBeatTime = DateTime.Now;
                            State = ListenerState.Connected;
                            Trace.TraceWarning("BilibiliListener: Reconnected.");
                            return;
                        }
                        else
                        {
                            Trace.TraceWarning("BilibiliListener: Retry after 3 seconds.");
                            Thread.Sleep(3000);
                        }
                    }
                };

                // Main loop
                while (!StopListenToken)
                {
                    Thread.Sleep(1000);
                    if (ws.ReadyState == WebSocketSharp.WebSocketState.Open && DateTime.Now.Subtract(LastSendHeartBeatTime).TotalMilliseconds >= HeartBeatDuration)
                    {
                        var heartbeat = PackageBuilder.MakeHeatBeat();
                        ws.Send(heartbeat.ToByteArray());
                        LastSendHeartBeatTime = DateTime.Now;
                        waitBack = true;
                    }
                    if (waitBack && DateTime.Now.Subtract(LastSendHeartBeatTime).TotalMilliseconds >= HeartBeatTimeout)
                    {
                        State = ListenerState.BadCommunication;
                        Trace.TraceWarning("BilibiliListener: Bad communication, retrying.");
                        OnBadCommunication?.Invoke(ws);
                    }
                }

                ws.Close();
                State = ListenerState.Disconnected;
            }
        }

        private async void Ws_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            waitBack = false;
            LastReceiveTime = DateTime.Now;

            foreach (var m in PackageParser.GetPackages(e.RawData))
            {
                try
                {
                    var msg = new BilibiliMessage(m);
                    if (msg.MsgType == MessageType.Unknown)
                    {
                        Trace.TraceInformation("BilibiliListener: MessageType: Unknown, skipped.");
                        Database.CollectSample(m);
                    }
                    else
                    {
                        await KeepMessage(new BilibiliMessage(m));
                    }
                }catch(Exception ex)
                {
                    Trace.TraceError(ex.Message);
                    Trace.TraceError(ex.StackTrace);
                    Trace.TraceError("================================");
                }
            }
        }

        private void SendHeartBeat(WebSocketSharp.WebSocket ws)
        {
            var package = new Package(MsgType.ClientHeart, "");
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

            //Call api to get user information.
            var headers = new Dictionary<string, string> {
                {             "User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36" },
                {                "Referer", "http://m.bilibili.com"},
                {                 "Origin", "http://m.bilibili.com" },
            };

            // Post and get data from API.
            var result = await HttpRequests.DownloadString(
                    url: "https://api.bilibili.com/x/space/acc/info?mid=" + uid,
                    headers: headers,
                    encoding: Encoding.UTF8
                );
            var json = JToken.Parse(result);
            var face64 = "";

            // Download avatar.
            if (json["data"]?["face"] != null)
            {
                var facedata = await HttpRequests.DownloadBytes(json["data"]["face"].ToString());
                face64 = ImageHelper.ConvertToJpegBase64(facedata);
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
