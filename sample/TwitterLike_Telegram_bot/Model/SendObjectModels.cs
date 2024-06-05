using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitterLike_Telegram_bot.Model
{
    public class SendMessageBodyBase
    {
        public String chat_id { get; set; }
        public String parse_mode { get; set; } = "MarkdownV2";
        public bool disable_notification { get; set; }
        public bool protect_content { get; set; }
        public String caption { get; set; }
    }

    public class GroupMediaItem
    {
        public String type { get; set; }
        public String media { get; set; }
        public String caption { get; set; }
        public bool has_spoiler { get; set; } = false;

    }

    public class SendTextBody : SendMessageBodyBase
    {
        public String text { get; set; }
    }

    public class SendPhotoBody : SendMessageBodyBase
    {
        public String photo { get; set; }
    }

    public class SendVideoBody : SendMessageBodyBase
    {
        public String video { get; set; }
    }

    public class SendFileBody : SendMessageBodyBase
    {
        public String document { get; set; }
    }

    public class SendGroupMediaBody
    {
        public String chat_id { get; set; }
        public GroupMediaItem[] media { get; set; }
    }
}
