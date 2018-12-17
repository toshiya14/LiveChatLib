using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiveChatLib
{
    public abstract class MessageBase
    {
        [JsonProperty(PropertyName = "avaurl")]
        public string AvatarUrl { get; private set; }
        [JsonProperty(PropertyName = "sender")]
        public string SenderName { get; private set; }

    }
}
