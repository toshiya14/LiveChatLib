using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace LiveChatLib
{
    public class PushService : IDisposable
    {
        public readonly WebSocketServer WsService;
        public readonly IList<IPushService> Services = new List<IPushService>();
        public readonly IList<Task> RunningTask = new List<Task>();
        
        public bool IsRunning;

        private static PushService _instance;
        private static object _instanceLock = new object();
        public static PushService Current
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null) {
                            _instance = new PushService();
                        }
                    }
                }
                return _instance;
            }
        }

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
        protected override void OnOpen()
        {
            var tasks = new List<Task>();
            foreach(var s in PushService.Current.Services)
            {
                tasks.Add(Task.Factory.StartNew(()=>s.OnWebSocketOpen(this.Sessions, this.ID)));
            }
            Task.WaitAll(tasks.ToArray());
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.Data.Equals("ping"))
            {
                Send("pong");
                return;
            }

            var json = JToken.Parse(e.Data);
            var flag = json["flag"].ToString();
            var tasks = new List<Task>();
            foreach (var s in PushService.Current.Services)
            {
                if (s.MessageFlag.Contains(flag))
                {
                    tasks.Add(Task.Factory.StartNew(() => s.OnReceiveMessage(this.Sessions, this.ID, json)));
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
        void OnWebSocketOpen(WebSocketSessionManager app, string id);
        void OnReceiveMessage(WebSocketSessionManager app, string id, JToken data);
        void OnWork(WebSocketServer server);
    }
}
