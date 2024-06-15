using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EbonCorvin
{
    namespace TiwtterTimelineParser
    {
        public class Tweet
        {
            public String Author { get; set; }
            public String TweetId { get; set; }
            public String Content { get; set; }
            public String CreateDate { get; set; }
            public String TweetUrl { get; set; }
            public Media[] Medias { get; set; }
            public String MediaJoined { get; set; }
        }

        public class Media
        {
            public String MediaType { get; set; }
            public String Url { get; set; }
        }

        public enum TimelineType
        {
            /// <summary>
            /// The Like timeline
            /// </summary>
            LikeTimeline,
            /// <summary>
            /// The home timeline, the one that is marked as "For you" on the home page
            /// </summary>
            HomeTimeline,
            /// <summary>
            /// The latest home timeline, the one that is marked as "Following" on the home page
            /// </summary>
            HomeLatetTimeline
        }

        public class TwitterLikeParser
        {
            private int ResultCount { get; set; } = 20;
            private TimelineFetcher Fetcher { get; set; }
            private TimelineType TimelineType;

            public String NextCursor { get; private set; }

            private readonly Regex REGEX_LIKE_ITEM = new Regex("(\\{\\\"entryId\\\".*?\\\"tweetDisplayType\\\":\\\"Tweet\\\"\\}.*?\\}\\})", RegexOptions.Compiled);
            private readonly Regex REGEX_LIKE_PIC_MEDIA = new Regex("\\\"extended_entities\\\"\\:\\{(.*?)},\\\"favorite_count\\\"", RegexOptions.Compiled);
            private readonly Regex REGEX_LIKE_VIDEO = new Regex("\\{\\\"bitrate\\\":(\\d+).*?\\\"url\\\":\\\"(.*?)\\\"\\}", RegexOptions.Compiled);
            private readonly Regex REGEX_NEXT_CURSOR = new Regex("\\{\\\"entryId\\\"\\:\\\"cursor\\-bottom\\-.*?\\\"value\\\"\\:\\\"(.*?)\\\"", RegexOptions.Compiled);
            private readonly Regex REGEX_LIKE_PIC = GetRegexForJsonValueExtraction("media_url_https");
            private readonly Regex REGEX_TWEET_CREATED_DATE = GetRegexForJsonValueExtraction("created_at");
            private readonly Regex REGEX_TWEET_SCREEN_NAME = GetRegexForJsonValueExtraction("screen_name");
            private readonly Regex REGEX_TWEET_TWEET_ID = GetRegexForJsonValueExtraction("conversation_id_str");
            private readonly Regex REGEX_TWEET_FULL_TEXT = GetRegexForJsonValueExtraction("full_text");
            public Tweet[] Tweets { get; set; }

            public TwitterLikeParser(TimelineType timelineType, String twitterId, String cookie, String csrtToken, int resultCount)
            {
                TimelineType = timelineType;
                Fetcher = new TimelineFetcher(twitterId, cookie, csrtToken, resultCount);
                ResultCount = resultCount;
                NextCursor = "null";
            }
            public bool FirstPage()
            {
                NextCursor = "null";
                return ParseTimeline(FetchTimeline());
            }
            public bool NextPage()
            {
                return ParseTimeline(FetchTimeline());
            }

            private string FetchTimeline()
            {
                switch (TimelineType)
                {
                    case TimelineType.LikeTimeline:
                        return Fetcher.FetchLikeTimeline(NextCursor);
                    case TimelineType.HomeTimeline:
                        return Fetcher.FetchHomeTimeline(NextCursor);
                    case TimelineType.HomeLatetTimeline:
                        return Fetcher.FetchHomeLatestTimeline(NextCursor);
                    default: return "";
                }
                
            }

            private bool ParseTimeline(String likePage)
            {
                List<Tweet> tweets = new List<Tweet>();
                var likeItems = REGEX_LIKE_ITEM.Matches(likePage);
                foreach (Match match in likeItems)
                {
                    String itemString = match.Groups[1].Value;
                    try
                    {
                        /** 
                         * So far 5 legacy objects are discovered, 
                         * 1. tweet creator information
                         * 2. (optional) quoted tweet creator information (inside quoted_status_result) 
                         * 3. (optional) quoted tweet tweet content (inside quoted_status_result) 
                         * 4. liked tweet content
                         * 5. (optional) tweet conversation control (Inside liked tweet content) 
                         * I should have written this library in python
                        **/
                        int tweetContentStart = itemString.LastIndexOf("\"legacy\":{\"bookmark_count\"");
                        List<Media> medias = new List<Media>();
                        if (itemString.IndexOf("\"type\":\"video\"", tweetContentStart) > -1 || itemString.IndexOf("\"type\":\"animated_gif\"", tweetContentStart) > -1)
                        {
                            // animated_gif has 0 bitrate
                            int maxBitrate = int.MinValue;
                            String maxBrUrl = null;
                            foreach (Match urlMatch in REGEX_LIKE_VIDEO.Matches(itemString, tweetContentStart))
                            {
                                int bitRate = int.Parse(urlMatch.Groups[1].Value);
                                String url = urlMatch.Groups[2].Value;
                                // Console.WriteLine("Bitrate: {0}, URL: {1}", bitRate, url);
                                if (bitRate > maxBitrate)
                                {
                                    maxBitrate = bitRate;
                                    maxBrUrl = url;
                                }
                            }
                            medias.Add(new Media() { MediaType = "video", Url = maxBrUrl });
                        }
                        if (itemString.IndexOf("\"type\":\"photo\"", tweetContentStart) > -1)
                        {
                            foreach (Match urlMatch in REGEX_LIKE_PIC.Matches(REGEX_LIKE_PIC_MEDIA.Match(itemString, tweetContentStart).Groups[1].Value))
                            {
                                String url = urlMatch.Groups[1].Value;
                                medias.Add(new Media() { MediaType = "photo", Url = url });
                            }
                        }
                        // Some like item has empty "tweet_results"
                        // Instead of trying to find all of them out, I ignore every post that doesn't have media
                        /*if (medias.Count == 0)
                            continue;*/
                        // bool hasQuote = itemString.Contains("quoted_status_result") && !itemString.Contains("tombstone");
                        String lastCreateDate = REGEX_TWEET_CREATED_DATE.Matches(itemString, tweetContentStart)[0].Groups[1].Value;
                        String screenName = REGEX_TWEET_SCREEN_NAME.Matches(itemString)[0].Groups[1].Value;
                        String tweetId = REGEX_TWEET_TWEET_ID.Matches(itemString, tweetContentStart)[0].Groups[1].Value;
                        String fullText = Regex.Unescape(REGEX_TWEET_FULL_TEXT.Matches(itemString, tweetContentStart)[0].Groups[1].Value);
                        String tweetUrl = "https://twitter.com/" + screenName + "/status/" + tweetId;
                        Tweet item = new Tweet()
                        {
                            CreateDate = lastCreateDate,
                            Author = screenName,
                            Content = fullText,
                            TweetId = tweetId,
                            TweetUrl = tweetUrl,
                            Medias = medias.ToArray(),
                            MediaJoined = String.Join("\r\n", (from Media media in medias select media.Url))
                        };
                        tweets.Add(item);
                    }
                    catch (Exception ex)
                    {
                        String fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString() + ".txt";
                        Console.WriteLine("Unable to process one of the post, check " + fileName + " for detail");
                        var writer = File.AppendText(fileName);
                        writer.AutoFlush = true;
                        writer.WriteLine(ex.Message);
                        writer.WriteLine("==========");
                        writer.WriteLine(ex.StackTrace);
                        writer.WriteLine("==========");
                        writer.WriteLine(itemString);
                        writer.Close();
                    }
                }
                Tweets = tweets.ToArray();
                Match nextCursorMatch = REGEX_NEXT_CURSOR.Match(likePage);
                String nextCursorText = nextCursorMatch.Groups[1].Value;
                NextCursor = "%22" + nextCursorText + "%22";
                return Tweets.Length > 0;
            }

            private static Regex GetRegexForJsonValueExtraction(String key)
            {
                // Match any characters that is not " and \
                // Or Match any character that start with \ (escaped)
                Regex regex = new Regex(String.Format("\\\"{0}\\\":\\\"((?:[^\"\\\\]|\\\\.)*)\\\"", key), RegexOptions.Compiled);
                return regex;
            }
        }
    }
}
