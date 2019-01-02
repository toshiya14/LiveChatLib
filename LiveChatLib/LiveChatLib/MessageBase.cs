using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiveChatLib
{
    public abstract class MessageBase
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        [JsonProperty(PropertyName = "avaurl")]
        public string AvatarUrl { get; set; }

        [JsonProperty(PropertyName = "sender")]
        public string SenderName { get; protected set; }

        [JsonProperty(PropertyName = "comment")]
        public string Comment { get; protected set; }

        [JsonProperty(PropertyName = "time")]
        public DateTime ReceiveTime { get; protected set; }

        [JsonIgnore]
        public string RawData { get; protected set; }

        public MessageBase()
        {
            this.ReceiveTime = DateTime.Now;
        }
    }
}
