using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitterLike_Telegram_bot.Model
{
    public class ChatType
    {
        public int id { get; set; }
        public bool is_bot { get; set; }
        public String first_name { get; set; }
        public String username { get; set; }
        public String language_code { get; set; }
        public String title { get; set; }
        public String type { get; set; }
    }
}
