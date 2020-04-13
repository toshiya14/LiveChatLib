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
        public Dictionary<string, string> Meta = new Dictionary<string, string>();

        [JsonProperty(PropertyName = "msgType")]
        public MessageType MsgType { get; private set; }

        [JsonProperty(PropertyName = "uid")]
        public int SenderId { get; private set; }

        [JsonProperty(PropertyName = "from")]
        public string From { get => "bili"; }

        public BilibiliMessage()
            : base() { }

        public BilibiliMessage(Package package)
        {
            this.Meta = new Dictionary<string, string>();
            this.RawData = package.Content;

            switch (package.MessageType)
            {
                case Bilibili.MsgType.Renqi:
                    var renqi = package.Body.ByteToInt32(true);
                    this.AvatarUrl = string.Empty;
                    this.Meta["type"] = "renqi";
                    this.Meta["time"] = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString();
                    this.Meta["renqi"] = renqi.ToString();
                    this.MsgType = MessageType.System;
                    this.SenderId = -1;
                    this.SenderName = "server";
                    break;

                case Bilibili.MsgType.ServerHeart:
                    this.AvatarUrl = string.Empty;
                    this.Meta["type"] = "heartbeat";
                    this.Meta["time"] = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString();
                    this.MsgType = MessageType.System;
                    this.SenderId = -1;
                    this.SenderName = "server";
                    break;

                case Bilibili.MsgType.Command:
                    var obj = JToken.Parse(package.Content.Trim());
                    switch (obj["cmd"].ToString().ToUpper())
                    {
                        case "WELCOME_GUARD":
                            this.Meta["uid"] = obj["data"]["uid"].ToString();
                            this.Meta["uname"] = obj["data"]["username"].ToString();
                            this.Meta["guard_level"] = obj["data"]["guard_level"].ToObject<int>().ToString();
                            this.SenderName = this.Meta["uname"];
                            this.MsgType = MessageType.Welcome;
                            this.SenderId = obj["data"]["uid"].ToObject<int>();
                            break;

                        case "WELCOME":
                            this.Meta["uid"] = obj["data"]["uid"].ToString();
                            this.Meta["uname"] = obj["data"]["uname"].ToString();
                            this.Meta["is_admin"] = obj["data"]["isadmin"]?.ToObject<bool>().ToString() ??
                                                    obj["data"]["is_admin"]?.ToObject<bool>().ToString() ?? "0";
                            this.Meta["is_vip"] = (obj["data"]["vip"] != null && obj["data"]["vip"].Value<int>() == 1).ToString();
                            this.Meta["is_svip"] = (obj["data"]["svip"] != null && obj["data"]["svip"].Value<int>() == 1).ToString();
                            this.SenderName = this.Meta["uname"];
                            this.MsgType = MessageType.Welcome;
                            this.SenderId = obj["data"]["uid"].ToObject<int>();
                            break;

                        case "SEND_GIFT":
                            this.Meta["uid"] = obj["data"]["uid"].ToString();
                            this.Meta["uname"] = obj["data"]["uname"].ToString();
                            this.Meta["face"] = obj["data"]["face"].ToString();
                            this.Meta["gift_name"] = obj["data"]["giftName"].ToString();
                            this.Meta["price"] = obj["data"]["price"].ToString();
                            this.Meta["num"] = obj["data"]["num"].Value<int>().ToString();
                            this.SenderName = this.Meta["uname"];
                            this.MsgType = MessageType.Gift;
                            this.AvatarUrl = this.Meta["face"];
                            this.SenderId = obj["data"]["uid"].ToObject<int>();
                            break;

                        case "PREPARING":
                            this.Meta["roomid"] = obj["roomid"].ToString();
                            this.SenderName = "server";
                            this.MsgType = MessageType.System;
                            break;

                        case "LIVE":
                            this.Meta["roomid"] = obj["roomid"].ToString();
                            this.SenderName = "server";
                            this.MsgType = MessageType.System;
                            break;

                        case "DANMU_MSG":
                            this.Meta["uname"] = obj["info"][2][1].ToString();
                            this.Meta["uid"] = obj["info"][2][0].ToString();
                            this.Meta["flag1"] = obj["info"][2][2].ToObject<int>().ToString();
                            this.Meta["flag2"] = obj["info"][2][3].ToObject<int>().ToString();
                            this.Meta["flag3"] = obj["info"][2][4].ToObject<int>().ToString();
                            this.Meta["timestamp"] = obj["info"][0][4].ToObject<long>().ToString();
                            this.Meta["msg"] = obj["info"][1].ToString();
                            this.SenderName = this.Meta["uname"];
                            this.MsgType = MessageType.Danmaku;
                            this.Comment = this.Meta["msg"];
                            this.SenderId = obj["info"][2][0].ToObject<int>();
                            break;

                        default:
                            this.MsgType = MessageType.Unknown;
                            break;
                    }
                    break;

                default:
                    this.MsgType = MessageType.Unknown;
                    break;
            }
        }
    }

    public enum MessageType
    {
        Unknown,
        Danmaku,
        Gift,
        Welcome,
        System
    }
}
