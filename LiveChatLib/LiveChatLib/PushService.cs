using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace LiveChatLib
{
    public class PushService : IDisposable
    {
        public readonly WebSocketServer WsService;
        public static readonly IList<IPushService> Services = new List<IPushService>();
        public static readonly IList<Task> RunningTask = new List<Task>();
        public bool IsRunning;

        public PushService()
        {
            WsService = new WebSocketServer(6099);
            WsService.AddWebSocketService<ChatLogApp>("/app");
        }

        public void OnLoad()
        {
            foreach(var s in Services)
            {
                s.OnServiceLoad();
            }
        }

        public void OnStop()
        {
            foreach(var s in Services)
            {
                s.OnServiceStop();
            }
            IsRunning = false;
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }
            WsService.Start();
            OnLoad();


            foreach (var s in Services)
            {
                Task.Factory.StartNew(()=>s.OnWork(WsService));
            }
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            OnStop();
        }

        public void Dispose()
        {
            WsService.Stop();
        }

        public void Register<T>() where T: IPushService, new()
        {
            var service = new T();
            Services.Add(service);
            if (IsRunning)
            {
                service.OnServiceLoad();
            }
        }
    }

    public class ChatLogApp : WebSocketBehavior
    {
        public void SendMessage(string message)
        {
            SendAsync(Encoding.UTF8.GetBytes(message), null);
        }

        protected override void OnOpen()
        {
            var tasks = new List<Task>();
            foreach(var s in PushService.Services)
            {
                tasks.Add(Task.Factory.StartNew(()=>s.OnWebSocketOpen(this.Sessions)));
            }
            Task.WaitAll(tasks.ToArray());
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var json = JToken.Parse(e.Data);
            var flag = json["flag"].ToString();
            var tasks = new List<Task>();
            foreach (var s in PushService.Services)
            {
                if (s.MessageFlag.Contains(flag))
                {
                    tasks.Add(Task.Factory.StartNew(() => s.OnReceiveMessage(this.Sessions, json["data"])));
                }
            }
            Task.WaitAll(tasks.ToArray());
        }
    }

    public interface IPushService
    {
        /// <summary>
        /// Set the message flags, if message contains one of these flags, OnReveciveMessage would be called.
        /// </summary>
        string[] MessageFlag { get; }

        /// <summary>
        /// Called when service loading.
        /// </summary>
        void OnServiceLoad();

        /// <summary>
        /// Called when service stopping.
        /// </summary>
        void OnServiceStop();

        /// <summary>
        /// Called when WebSocket open.
        /// </summary>
        /// <param name="app">The application.</param>
        void OnWebSocketOpen(WebSocketSessionManager app);

        /// <summary>
        /// Called when receiving message.
        /// </summary>
        /// <param name="app">The WebSocketSessionManager instance.</param>
        /// <param name="data">The message body.</param>
        void OnReceiveMessage(WebSocketSessionManager app, JToken data);

        /// <summary>
        /// Main working loop.
        /// </summary>
        /// <param name="server">The server.</param>
        void OnWork(WebSocketServer server);
    }
}
