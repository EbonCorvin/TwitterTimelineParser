using System;

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
