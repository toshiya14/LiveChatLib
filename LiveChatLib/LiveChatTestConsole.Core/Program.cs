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
            PushService.Current.Register<BilibiliPushService>();
            PushService.Current.Start();

            Console.ReadKey();
            PushService.Current.Stop();
        }
    }
}
