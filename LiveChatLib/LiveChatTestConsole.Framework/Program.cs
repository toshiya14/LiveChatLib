using LiveChatLib;
using LiveChatLib.Bilibili;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveChatTestConsole.Framework
{
    class Program
    {
        static void Main(string[] args)
        {

            PushService.Current.Register<BilibiliPushService>();
            PushService.Current.Start();
            Console.WriteLine("Started!");

            Console.ReadKey();
            PushService.Current.Stop();
        }
    }
}
