using LiveChatLib.Bilibili.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp.Server;

namespace LiveChatLib.Bilibili
{
    public class BilibiliPushService : IPushService
    {
        public string[] MessageFlag
        {
            get
            {
                return new string[] { "bilibili", "general" };
            }
        }

        /// <summary>
        /// Set StopToken to true, to stop looping after current working loop.
        /// </summary>
        public bool StopToken = false;

        public void OnReceiveMessage(WebSocketSessionManager app, string cid, JToken data)
        {
            if (data["type"] != null && data["type"].ToString().Equals("query"))
            {
                if (data["query"] != null && data["query"].ToString().Equals("face"))
                {
                    var id = data["id"]?.ToObject<int>() ?? 0;
                    if (id <= 0)
                    {
                        return;
                    }
                    var user = Database.PickUserInformation(id);
                    if (!string.IsNullOrEmpty(user.FaceBase64))
                    {
                        app.SendTo(JsonConvert.SerializeObject(
                            new
                            {
                                type = "user",
                                data = new[] { user }
                            }
                        ), cid);
                    }
                }
            }
        }
        public void OnServiceLoad()
        {
            StopToken = false;
        }
        public void OnServiceStop()
        {
            StopToken = true;
        }
        public void OnWebSocketOpen(WebSocketSessionManager app, string cid)
        {
            var results = Database.FetchLatestComments(5);
            app.Broadcast(JsonConvert.SerializeObject(new { type = "msg", data = results }));
        }

        public void OnWork(WebSocketServer server)
        {
            var listener = new BilibiliListener();
            listener.OnProcessMessage +=
                message =>
                {
                    server.WebSocketServices["/app"].Sessions.Broadcast(
                        JsonConvert.SerializeObject(
                            new
                            {
                                type = "msg",
                                data = new[] { message }
                            }
                        )
                    );
                };

            listener.OnProcessUser +=
                user =>
                {
                    server.WebSocketServices["/app"].Sessions.Broadcast(
                        JsonConvert.SerializeObject(
                            new
                            {
                                type = "user",
                                data = new[] { user }
                            }
                        )
                    );
                };

            listener.OnBadCommunication +=
                ws =>
                {
                    server.WebSocketServices["/app"].Sessions.Broadcast(
                        JsonConvert.SerializeObject(
                            new
                            {
                                type = "sys",
                                data = new[] { new { msg = "彈幕引擎與伺服器斷開連結! TwT" } }
                            }
                        )
                    );
                };
                
            listener.LoopListening(ref StopToken);
        }
    }
}
