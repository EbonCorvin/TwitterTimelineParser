using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net;
using TwitterLike_Telegram_bot.Model;
using System.IO;
using Twitter_Download;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace TwitterLike_Telegram_bot
{
    class Program
    {
        static void Main(string[] args)
        {
            TelegramBot bot = new TelegramBot();
            bot.Start();
            Console.Read();
        }
    }
}
