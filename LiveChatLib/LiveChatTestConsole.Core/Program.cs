using System;
using System.Collections.Generic;
using System.Text;
using LiveChatLib;
using LiveChatLib.Bilibili;
using LiveChatLib.Helpers;

namespace LiveChatTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var ps = new PushService())
            {
                ps.Register<BilibiliPushService>();
                ps.Start();

                Console.ReadKey();
                ps.Stop();
            }
        }
    }
}
