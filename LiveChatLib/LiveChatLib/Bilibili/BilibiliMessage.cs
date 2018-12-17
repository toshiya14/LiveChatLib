using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiveChatLib.Bilibili
{
    public class BilibiliMessage : MessageBase
    {
        [JsonProperty(PropertyName = "meta")]
        public Dictionary<string, string> meta;
        [JsonProperty(PropertyName = "msgType")]
        public MessageType MsgType { get; private set; }
    }

    public enum MessageType
    {
        Danmaku,
        Gift,
        Welcome,
        System
    }
}
