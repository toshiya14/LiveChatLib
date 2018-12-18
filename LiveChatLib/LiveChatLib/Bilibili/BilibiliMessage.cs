using LiveChatLib.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace LiveChatLib.Bilibili
{
    public class BilibiliMessage : MessageBase
    {
        [JsonProperty(PropertyName = "meta")]
        public Dictionary<string, string> meta = new Dictionary<string, string>();
        [JsonProperty(PropertyName = "msgType")]
        public MessageType MsgType { get; private set; }

        public BilibiliMessage()
            : base() { }

        public BilibiliMessage(Package package)
        {
            this.meta = new Dictionary<string, string>();

            switch (package.MessageType)
            {
                case Bilibili.MsgType.Renqi:
                    var renqi = package.Body.ByteToInt32(true);
                    this.AvatarUrl = string.Empty;
                    this.meta["type"] = "renqi";
                    this.meta["time"] = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString();
                    this.meta["renqi"] = renqi.ToString();
                    this.MsgType = MessageType.System;
                    this.SenderName = "server";
                    break;

                case Bilibili.MsgType.ServerHeart:
                    this.AvatarUrl = string.Empty;
                    this.meta["type"] = "heartbeat";
                    this.meta["time"] = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString();
                    this.MsgType = MessageType.System;
                    this.SenderName = "server";
                    break;

                case Bilibili.MsgType.Command:
                    var obj = JToken.Parse(package.Content);
                    switch (obj["cmd"].ToString().ToUpper())
                    {
                        case "WELCOME_GUARD":
                            this.meta["uid"] = obj["data"]["uid"].ToString();
                            this.meta["uname"] = obj["data"]["username"].ToString();
                            this.meta["guard_level"] = obj["data"]["guard_level"].ToObject<int>().ToString();
                            this.SenderName = this.meta["uname"];
                            this.MsgType = MessageType.Welcome;
                            break;

                        case "WELCOME":
                            this.meta["uid"] = obj["data"]["uid"].ToString();
                            this.meta["uname"] = obj["data"]["uname"].ToString();
                            this.meta["is_admin"] = obj["data"]["is_admin"].ToObject<bool>().ToString();
                            this.meta["is_vip"] = (obj["data"]["vip"] != null && obj["data"]["vip"].Value<int>() == 1).ToString();
                            this.meta["is_svip"] = (obj["data"]["svip"] != null && obj["data"]["svip"].Value<int>() == 1).ToString();
                            this.SenderName = this.meta["uname"];
                            this.MsgType = MessageType.Welcome;
                            break;

                        case "SEND_GIFT":
                            this.meta["uid"] = obj["data"]["uid"].ToString();
                            this.meta["uname"] = obj["data"]["uname"].ToString();
                            this.meta["face"] = obj["data"]["face"].ToString();
                            this.meta["gift_name"] = obj["data"]["giftName"].ToString();
                            this.meta["price"] = obj["data"]["price"].ToString();
                            this.meta["num"] = obj["data"]["num"].Value<int>().ToString();
                            this.SenderName = this.meta["uname"];
                            this.MsgType = MessageType.Gift;
                            this.AvatarUrl = this.meta["face"];
                            break;
                    }
                    break;
            }
        }
    }

    public enum MessageType
    {
        Danmaku,
        Gift,
        Welcome,
        System
    }
}
