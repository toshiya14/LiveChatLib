using Newtonsoft.Json;
using System;

namespace LiveChatLib.Bilibili.Storage
{
    public class User
    {
        [JsonProperty(PropertyName = "mid")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "sex")]
        public string Sex { get; set; }

        [JsonProperty(PropertyName = "face")]
        public string Face { get; set; }

        [JsonProperty(PropertyName = "birth")]
        public string BirthDay { get; set; }

        [JsonProperty(PropertyName = "lv")]
        public int Level { get; set; }

        [JsonProperty(PropertyName = "uptime")]
        public DateTime LastUpdateTime { get; set; }
    }
}
