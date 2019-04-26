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
                    if (!string.IsNullOrEmpty(user?.FaceBase64))
                    {
                        app.SendTo(JsonConvert.SerializeObject(
                            new
                            {
                                type = "user",
                                data = new[] { user }
                            },
                            Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            }
                        ), cid);
                    }
                }
            }

            if (data["type"] != null && data["type"].ToString().Equals("broadcast"))
            {
                var json = JsonConvert.SerializeObject(
                            data["body"],
                            Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                var token = data["token"]?.ToString();
                if (token.Equals("toshiya14"))
                {
                    app.Broadcast(json);
                }
                else
                {
                    app.SendTo("{\"code\":500,\"msg\":\"Server refused your action.\"}", cid);
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
            var results = Database.FetchLatestComments(5).Result;
            app.SendTo(JsonConvert.SerializeObject(
                            new { type = "msg", data = results },
                            Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            }
                      ), cid);
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
                            },
                            Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
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
                            },
                            Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
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
                            },
                            Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            }
                        )
                    );
                };

            listener.LoopListening(ref StopToken);
        }
    }
}
