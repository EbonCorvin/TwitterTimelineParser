using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using EbonCorvin;
using System.Linq;
using EbonCorvin.TiwtterTimelineParser;

namespace TwitterLike_Telegram_bot
{
    internal class TelegramBot
    {
        private static String channelName = null;
        private HashSet<int> hashTable;
        private FileStream posted;
        private FileStream skipped;
        private ConfigLoader config;
        private int checkInterval = 0;
        public TelegramBot()
        {
            hashTable = new HashSet<int>();
            posted = new FileStream("posted.bin", FileMode.OpenOrCreate);
            byte[] buffer = new byte[4];
            while (posted.Position < posted.Length)
            {
                posted.Read(buffer, 0, 4);
                hashTable.Add(BitConverter.ToInt32(buffer, 0));
            }
            skipped = new FileStream("skipped.bin", FileMode.OpenOrCreate);
            while (skipped.Position < skipped.Length)
            {
                skipped.Read(buffer, 0, 4);
                hashTable.Add(BitConverter.ToInt32(buffer, 0));
            }

            config = new ConfigLoader("config.txt");
            channelName = config["telegram_channel"];
            if (!channelName.StartsWith("@"))
                channelName = "@" + channelName;
            TelegramApi.SetApiKey(config["telegram_bot_apikey"]);
            if (!int.TryParse(config["check_interval"], out checkInterval))
            {
                checkInterval = 120000;
            }
            else
            {
                checkInterval *= 1000;
            }
        }
        public void Start()
        {
            Console.WriteLine("Welcome to the Twitter like posts fowarding Telegram bot.");
            Console.WriteLine("This bot checks the liked post list of your account every {0} seconds,", checkInterval / 1000);
            Console.WriteLine("and forwards them to the Telegram channel {0}.", channelName);
            TwitterLikeParser likeList = new TwitterLikeParser(TimelineType.LikeTimeline, config["twitter_id"], config["cookie"], config["csrt"], 50);
            while (true)
            {
                try
                {
                    likeList.FirstPage();
                    ProcessTweetBatch(likeList.Tweets);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    posted.FlushAsync();
                    skipped.FlushAsync();
                    Thread.Sleep(checkInterval);
                }
            }
        }

        private void ProcessTweetBatch(Tweet[] tweets, int fallBackIndex = -1)
        {
            int count = 0;
            for (int i = 0; i < tweets.Length;)
            {
                if (fallBackIndex != -1 && i != fallBackIndex)
                {
                    i++;
                    continue;
                }
                Tweet tweet = tweets[i];
                int hashCode = GetStableHashCode(tweet.TweetId);
                if (!hashTable.Contains(hashCode))
                {
                    try
                    {
                        if (fallBackIndex != -1)
                        {
                            var mediaUrls = from Media media in tweet.Medias
                                            select media.Url;
                            String content = String.Join("\r\n", mediaUrls);
                            content += "\r\n\r\n" + tweet.Content;
                            TelegramApi.SendText(channelName, content);
                        }
                        else
                        if (tweet.Medias.Length > 0)
                        {
                            if (tweet.Medias.Length > 1)
                                TelegramApi.SendGroupMedia(channelName, tweet.Medias, tweet.Content);
                            else
                            {
                                if (tweet.Medias[0].MediaType == "photo")
                                {
                                    TelegramApi.SendPhoto(channelName, tweet.Medias[0].Url+":orig", tweet.Content);
                                }
                                else if (tweet.Medias[0].MediaType == "video")
                                {
                                    TelegramApi.SendVideo(channelName, tweet.Medias[0].Url, tweet.Content);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("This tweet doesn't contain any media: {0}", tweet.TweetUrl);
                        };
                        count++;
                        hashTable.Add(hashCode);
                        posted.Write(BitConverter.GetBytes(hashCode), 0, 4);
                        if (fallBackIndex != -1)
                            return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error when processing this tweet: {0}", tweet.TweetUrl);
                        if (ex is WebException)
                        {
                            WebException webex = (WebException)ex;
                            if (webex.Response == null)
                            {
                                Console.WriteLine("Network error: {0}", ex.Message);
                                break;
                            }
                            using (var stream = webex.Response.GetResponseStream())
                            using (var reader = new StreamReader(stream))
                            {
                                String responseJson = reader.ReadToEnd();
                                var json = JsonSerializer.Deserialize<JsonObject>(responseJson);
                                int errorCode = json["error_code"].GetValue<int>();
                                if (errorCode == 429)
                                {
                                    int retryPause = json["parameters"]["retry_after"].GetValue<int>();
                                    Console.WriteLine("Too many request, take a break for {0} seconds...", retryPause);
                                    Thread.Sleep(retryPause * 1000);
                                    continue;
                                }
                                else
                                {

                                    Console.WriteLine("Error returned from Telegram API!");
                                    Console.WriteLine(responseJson);
                                    Console.WriteLine();
                                    hashTable.Add(hashCode);
                                    skipped.Write(BitConverter.GetBytes(hashCode), 0, 4);
                                    File.WriteAllText(hashCode.ToString("X8") + ".txt", tweet.TweetUrl + "\r\n" + tweet.MediaJoined + "\r\n" + responseJson);
                                    // Sometimes file cannot be sent, probably because it's too big
                                    // 400 Bad Request: wrong file identifier/HTTP URL specified
                                    /*if (responseJson.Contains("Bad Request: wrong file identifier/HTTP URL specified"))
                                    {
                                        if (fallBackIndex == -1)
                                        {
                                            Console.WriteLine("Sending the tweet media as link instead");
                                            ProcessTweetBatch(tweets, i);
                                        }
                                        else return;
                                    }*/
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
                i++;
            }
            if (count > 0)
                Console.WriteLine("{1:yyyy-MM-dd HH:mm:ss} - Followed {0} liked post(s)", count, DateTime.Now);
        }

        private static int GetStableHashCode(string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
