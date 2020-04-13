using Newtonsoft.Json;
using System.Text;

namespace LiveChatLib.Bilibili
{
    public static class PackageBuilder
    {
        public static Package MakeAuthPackage(long uid, long roomid, string token)
        {
            var body = new {
                uid = uid,
                roomid = roomid,
                protover = 2,
                platform = "web",
                clientver= "1.10.6",
                key = token
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
