using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using TwitterLike_Telegram_bot.Model;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Twitter_Download;
using EbonCorvin.TwitterLike;

namespace TwitterLike_Telegram_bot
{
    public class TelegramApi
    {
        private const String TELEGRAM_API_URL = "https://api.telegram.org/bot{0}/{1}";
        private static String apikey = null;

        public static void SetApiKey(String key)
        {
            apikey = key;
        }

        public static void SendMessage(String target, String content, SendTextBody options = null)
        {
            if (options == null)
                options = new SendTextBody();
            options.chat_id = target;
            options.text = EscapeText(content);
            String reqBody = JsonSerializer.Serialize(options, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            SendRequest("sendMessage", reqBody);
        }

        private static String EscapeText(String text)
        {
            Regex regex = new Regex("([_*\\[\\]()~`>#+-=|{}.!])", RegexOptions.Compiled);
            return regex.Replace(text, new MatchEvaluator((m)=>"\\"+m.Value));
        }

        public static void SendPhoto(String target, String imageUrl, String caption, SendPhotoBody options = null)
        {
            if (options == null)
                options = new SendPhotoBody();
            options.chat_id = target;
            options.caption = EscapeText(caption);
            options.photo = imageUrl;
            String reqBody = JsonSerializer.Serialize(options, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            SendRequest("sendPhoto", reqBody);
        }

        public static void SendVideo(String target, String videoUrl, String caption, SendVideoBody options = null)
        {
            if (options == null)
                options = new SendVideoBody();
            options.chat_id = target;
            options.caption = EscapeText(caption);
            options.video = videoUrl;
            String reqBody = JsonSerializer.Serialize(options, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            SendRequest("sendVideo", reqBody);
        }

        public static void SendGroupMedia(String target, Media[] medias, String caption, SendGroupMediaBody options = null)
        {
            if (options == null)
                options = new SendGroupMediaBody();
            options.chat_id = target;
            GroupMediaItem[] items = new GroupMediaItem[medias.Length];
            for (int i = 0; i < medias.Length; i++)
            {
                items[i] = new GroupMediaItem()
                {
                    type = medias[i].MediaType,
                    media = medias[i].Url,
                    caption = caption
                };
            }
            options.media = items;
            String reqBody = JsonSerializer.Serialize(options, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            SendRequest("sendMediaGroup", reqBody);
        }

        public static void SendText(String target, String text, SendTextBody options = null)
        {
            if (options == null)
                options = new SendTextBody();
            options.chat_id = target;
            options.text = EscapeText(text);
            String reqBody = JsonSerializer.Serialize(options, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            SendRequest("sendMessage", reqBody);
        }

        public static void SendRequest(String endpoint, String json)
        {
            WebRequest request = WebRequest.Create(String.Format(TELEGRAM_API_URL, apikey, endpoint));
            request.Method = "POST";
            request.ContentType = "application/json";
            byte[] encodedJson = Encoding.UTF8.GetBytes(json);
            Stream body = request.GetRequestStream();
            body.Write(encodedJson, 0, encodedJson.Length);
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            String responseJson = reader.ReadToEnd();
            // Console.WriteLine(responseJson);
        }
    }
}
