using LiveChatLib.Bilibili.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp.Server;

namespace LiveChatLib.Bilibili
{
    public class BilibiliPushService : IPushService
    {
        public string[] MessageFlag {
            get {
                //return new string[] { "bilibili", "general" };
                return new string[0];
            }
        }

        public bool StopToken = false;

        public void OnReceiveMessage(WebSocketSessionManager app, JToken data)
        {
            // Do Nothing.
        }
        public void OnServiceLoad()
        {
            StopToken = false;
        }
        public void OnServiceStop()
        {
            StopToken = true;
        }
        public void OnWebSocketOpen(WebSocketSessionManager app)
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
            listener.LoopListening(ref StopToken);
        }
    }
}
