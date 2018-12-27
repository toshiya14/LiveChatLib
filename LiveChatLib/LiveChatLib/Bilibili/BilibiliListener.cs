using LiveChatLib.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace LiveChatLib.Bilibili
{
    class BilibiliListener : ILiveChatListener
    {
        public int LiveRoomID { get; set; }
        public bool StopListenToken { get; set; }
        public FixedSizedQueue<BilibiliMessage> MessageQueue { get; set; }

        public async Task KeepMessage(MessageBase message)
        {
            var bmsg = message as BilibiliMessage;
            if (bmsg.MsgType != MessageType.System)
            {
                if (string.IsNullOrEmpty(bmsg.AvatarUrl))
                {
                    if (bmsg.meta["uid"] != null) {

                    }
                }
            }
        }

        public async Task LoopListening()
        {
            // Get Room ID.
            var json = await DownloadString(@"https://api.live.bilibili.com/room/v1/Room/room_init?" + LiveRoomID);
            var jobj = JToken.Parse(json);
            var id = jobj["data"]["room_id"].ToObject<int>();
            
            using (var ws = new WebSocketSharp.WebSocket("wss://broadcastlv.chat.bilibili.com/sub"))
            {
                ws.OnMessage += Ws_OnMessage;
                while (!StopListenToken)
                {
                    
                }
            }
        }

        private async void Ws_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            foreach (var m in PackageParser.GetPackages(e.RawData))
            {
                await KeepMessage(new BilibiliMessage(m));
            }
        }

        private async Task<string> DownloadString(string url)
        {
            string result;
            using (var client = new WebClient())
            {
                result = await client.DownloadStringTaskAsync(url);
            }
            return result;
        }

        private void SendHeartBeat(WebSocketSharp.WebSocket ws)
        {
            var package = new Package(MsgType.ClientHeart, new byte[0]);
            var data = package.Body;
            ws.SendAsync(data, null);
        }

        private void CacheFace(string uid)
        {
            
        }
    }
}
