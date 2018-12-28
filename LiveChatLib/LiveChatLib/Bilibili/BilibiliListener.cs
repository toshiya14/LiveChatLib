using LiveChatLib.Bilibili.Storage;
using LiveChatLib.Common;
using LiveChatLib.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiveChatLib.Bilibili
{
    class BilibiliListener : ILiveChatListener
    {
        public int LiveRoomID { get; set; }
        public FixedSizedQueue<BilibiliMessage> MessageQueue { get; set; }
        public delegate void ProcessMessageHandler(MessageBase message);
        public event ProcessMessageHandler OnProcessMessage;


        public BilibiliListener()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config/bilibili.config");
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
            if (bmsg.MsgType != MessageType.System)
            {
                if (string.IsNullOrEmpty(bmsg.AvatarUrl))
                {
                    if (bmsg.Meta.ContainsKey("uid"))
                    {
                        await CacheUser(bmsg.Meta["uid"]);
                    }
                }
            }
            Database.KeepMessage(bmsg);
            OnProcessMessage(message);
        }

        public void LoopListening(ref bool StopListenToken)
        {
            // Get Room ID.
            var json = HttpRequests.DownloadString(@"https://api.live.bilibili.com/room/v1/Room/room_init?id=" + LiveRoomID).Result;
            var jobj = JToken.Parse(json);
            var id = jobj["data"]["room_id"].ToObject<int>();

            using (var ws = new WebSocketSharp.WebSocket("wss://broadcastlv.chat.bilibili.com/sub"))
            {
                ws.Connect();
                // Initialize
                var package = PackageBuilder.MakeAuthPackage(0, id);
                ws.OnMessage += Ws_OnMessage;

                var bytes = package.ToByteArray();
                ws.Send(bytes);

                while (!StopListenToken)
                {
                    Thread.Sleep(30000);
                    var heartbeat = PackageBuilder.MakeHeatBeat();
                    ws.Send(heartbeat.ToByteArray());
                }
                ws.Close();
            }
        }

        private async void Ws_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
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

        private async Task<User> CacheUser(string uid)
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

            // Save the data.
            var json = JToken.Parse(result);
            var user = new User
            {
                BirthDay = json["data"]["birthday"]?.ToString() ?? "保密",
                Face = json["data"]["face"]?.ToString() ?? "保密",
                Level = json["data"]["level_info"]?["current_level"]?.ToObject<int>() ?? -1,
                Id = json["data"]["mid"]?.ToObject<int>() ?? -1,
                Name = json["data"]["name"]?.ToString() ?? "保密",
                Sex = json["data"]["sex"]?.ToString() ?? "保密"
            };
            Database.SaveUserInformation(user);
            return user;
        }
    }
}
