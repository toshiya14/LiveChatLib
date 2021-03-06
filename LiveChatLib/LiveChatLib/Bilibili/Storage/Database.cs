using LiteDB;
using LiveChatLib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LiveChatLib.Bilibili.Storage
{
    public class Database
    {
        private static readonly string DatabaseHomeFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"db");
        private static readonly string UserDatabasePath = Path.Combine(DatabaseHomeFolder, @"user.db");
        private static readonly string ChatLogDatabasePath = Path.Combine(DatabaseHomeFolder, @"chatlog/" + DateTime.Now.ToString("yyyy-MM-dd") + @".db");
        private static readonly string SampleDatabasePath = Path.Combine(DatabaseHomeFolder, @"samples/" + DateTime.Now.ToString("yyyy-MM-dd") + @".db");

        /// <summary>
        /// Saves the user information.
        /// </summary>
        /// <param name="user">The user.</param>
        public static void SaveUserInformation(User user)
        {
            var fi = new FileInfo(UserDatabasePath);
            fi.Directory.Create();

            using (var db = new LiteDatabase(UserDatabasePath))
            {
                var users = db.GetCollection<User>("users");
                var results = users.Find(x => x.Id == user.Id);
                if (results.Count() == 0)
                {
                    users.Insert(user);
                    users.EnsureIndex(x => x.Id);
                    users.EnsureIndex(x => x.Name);
                }
                else
                {
                    var toUpdate = results.First();
                    toUpdate.BirthDay = user.BirthDay;
                    toUpdate.Face = user.Face;
                    toUpdate.Level = user.Level;
                    toUpdate.Name = user.Name;
                    toUpdate.Sex = user.Sex;
                    users.Update(toUpdate);
                }
            }

        }

        /// <summary>
        /// Picks the user information with mid.
        /// </summary>
        /// <param name="mid">The mid.</param>
        /// <returns></returns>
        public static User PickUserInformation(int mid)
        {
            var fi = new FileInfo(UserDatabasePath);
            fi.Directory.Create();

            using (var db = new LiteDatabase(UserDatabasePath))
            {
                var users = db.GetCollection<User>("users");
                var results = users.Find(x => x.Id == mid);
                if (results.Count() == 0)
                {
                    return null;
                }
                else
                {
                    return results.First();
                }
            }

        }

        public static void KeepMessage(BilibiliMessage message)
        {
            var fi = new FileInfo(ChatLogDatabasePath);
            fi.Directory.Create();

            using (var db = new LiteDatabase(ChatLogDatabasePath))
            {
                var chats = db.GetCollection<BilibiliMessage>();
                chats.Insert(message);
                chats.EnsureIndex(x => x.SenderName);
                chats.EnsureIndex(x => x.ReceiveTime);
                chats.EnsureIndex(x => x.SenderId);
            }

        }

        public static void CollectSample(Package package)
        {
            var fi = new FileInfo(SampleDatabasePath);
            fi.Directory.Create();

            using(var db = new LiteDatabase(SampleDatabasePath))
            {
                var cols = db.GetCollection<Package>();
                cols.Insert(package);
            }
        }

        public static async Task<IList<BilibiliMessage>> FetchLatestComments(int count)
        {
            var fi = new FileInfo(ChatLogDatabasePath);
            fi.Directory.Create();
            fi = new FileInfo(UserDatabasePath);
            fi.Directory.Create();

            using (var db = new LiteDatabase(ChatLogDatabasePath))
            {
                var chats = db.GetCollection<BilibiliMessage>();
                var query = Query.And(
                               Query.All("ReceiveTime", Query.Descending),
                               Query.Or(
                                   Query.EQ("MsgType", "Danmaku"),
                                   Query.EQ("MsgType", "Gift")
                               )
                            );
                var results = chats.Find(query, 0, count);

                foreach (var i in results)
                {
                    var user = PickUserInformation(i.SenderId);
                    if (user != null)
                    {
                        var facedata = await HttpRequests.DownloadBytes(user.Face);
                        i.AvatarBase64 = ImageHelper.ConvertToJpegBase64(facedata);
                    }
                }

                return results.Reverse().ToList();
            }
        }
    }
}
