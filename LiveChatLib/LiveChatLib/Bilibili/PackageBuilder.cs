using Newtonsoft.Json;
using System.Text;

namespace LiveChatLib.Bilibili
{
    public static class PackageBuilder
    {
        public static Package MakeAuthPackage(long uid, long roomid)
        {
            var body = new {
                uid = uid,
                roomid = roomid,
                protover = 1,
                platform = "web",
                clientver = "1.5.10.1"
            };
            var package = new Package(MsgType.Auth, JsonConvert.SerializeObject(body,Formatting.None,new JsonSerializerSettings{NullValueHandling = NullValueHandling.Ignore}));
            return package;
        }

        public static Package MakeHeatBeat()
        {
            var package = new Package(MsgType.ClientHeart, "{}");
            return package;
        }
    }
}
