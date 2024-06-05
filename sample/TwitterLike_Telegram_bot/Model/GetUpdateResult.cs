using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitterLike_Telegram_bot.Model
{
    public class MessageBody
    {
        public int messageId { get; set; }
        public ChatType from { get; set; }
        public ChatType chat { get; set; }
        public int date { get; set; }
        public String text { get; set; }

    }

    public class ResultItem
    {
        public int updateId { get; set; }
        public MessageBody message { get; set; }
    }

    public class GetUpdateResult
    {
        public bool ok { get; set; }
        public int testint { get; set; }
        public ResultItem[] result { get; set; }
    }
}
