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
    namespace TwitterLike
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

        public class TwitterLikeParser
        {
            private const String STR_TWITTER_LIKE_URL = "https://api.twitter.com/graphql/p3ELQstq2ZEbEID4yu9R1A/Likes";
            private const String STR_TWITTER_BEARER_KEY = "Bearer AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";
            // 0: Twitter user ID, 1: Return count, 2: Result cursor
            private const String STR_QUERY_VARIABLE = "%7B%22userId%22%3A%22{0}%22%2C%22count%22%3A{1}%2C%22cursor%22%3A{2}%2C%22includePromotedContent%22%3Afalse%2C%22withSuperFollowsUserFields%22%3Atrue%2C%22withDownvotePerspective%22%3Afalse%2C%22withReactionsMetadata%22%3Afalse%2C%22withReactionsPerspective%22%3Afalse%2C%22withSuperFollowsTweetFields%22%3Atrue%2C%22withClientEventToken%22%3Afalse%2C%22withBirdwatchNotes%22%3Afalse%2C%22withVoice%22%3Atrue%2C%22withV2Timeline%22%3Atrue%7D";
            private const String STR_QUERY_FEATURE = "%7B%22responsive_web_twitter_blue_verified_badge_is_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%2C%22view_counts_public_visibility_enabled%22%3Atrue%2C%22longform_notetweets_consumption_enabled%22%3Afalse%2C%22tweetypie_unmention_optimization_enabled%22%3Atrue%2C%22responsive_web_uc_gql_enabled%22%3Atrue%2C%22vibe_api_enabled%22%3Atrue%2C%22responsive_web_edit_tweet_api_enabled%22%3Atrue%2C%22graphql_is_translatable_rweb_tweet_is_translatable_enabled%22%3Atrue%2C%22view_counts_everywhere_api_enabled%22%3Atrue%2C%22standardized_nudges_misinfo%22%3Atrue%2C%22tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled%22%3Afalse%2C%22interactive_text_enabled%22%3Atrue%2C%22responsive_web_text_conversations_enabled%22%3Afalse%2C%22responsive_web_enhance_cards_enabled%22%3Afalse%7D%0A";
            private const int INT_RESULT_COUNT = 50;
            private String UserId { get; set; }
            private String Cookie { get; set; }
            private String CSRT_Token { get; set; }
            private WebClient Client { get; set; }
            public String NextCursor { get; private set; }

            private readonly Regex REGEX_LIKE_ITEM = new Regex("(\\{\\\"entryId\\\".*?\\\"tweetDisplayType\\\":\\\"Tweet\\\"\\}\\}\\})", RegexOptions.Compiled);
            private readonly Regex REGEX_LIKE_PIC_MEDIA = new Regex("\\\"extended_entities\\\"\\:\\{(.*?)},\\\"favorite_count\\\"", RegexOptions.Compiled);
            private readonly Regex REGEX_LIKE_VIDEO = new Regex("\\{\\\"bitrate\\\":(\\d+).*?\\\"url\\\":\\\"(.*?)\\\"\\}", RegexOptions.Compiled);
            private readonly Regex REGEX_NEXT_CURSOR = new Regex("\\{\\\"entryId\\\"\\:\\\"cursor\\-bottom\\-.*?\\\"value\\\"\\:\\\"(.*?)\\\"", RegexOptions.Compiled);
            private readonly Regex REGEX_LIKE_PIC = GetRegexForJsonValueExtraction("media_url_https");
            private readonly Regex REGEX_TWEET_CREATED_DATE = GetRegexForJsonValueExtraction("created_at");
            private readonly Regex REGEX_TWEET_SCREEN_NAME = GetRegexForJsonValueExtraction("screen_name");
            private readonly Regex REGEX_TWEET_TWEET_ID = GetRegexForJsonValueExtraction("conversation_id_str");
            private readonly Regex REGEX_TWEET_FULL_TEXT = GetRegexForJsonValueExtraction("full_text");
            public Tweet[] Tweets { get; set; }
            public TwitterLikeParser(String twitterId, String cookie, String csrtToken)
            {
                NextCursor = "null";
                UserId = twitterId;
                Cookie = cookie;
                CSRT_Token = csrtToken;

                Client = new WebClient();
                Client.Headers.Add("x-csrf-token", CSRT_Token);
                Client.Headers.Add("Cookie", Cookie);
                Client.Headers.Add("Authorization", STR_TWITTER_BEARER_KEY);
                Client.Encoding = Encoding.UTF8;
            }
            public bool FirstPage()
            {
                NextCursor = "null";
                return GetAndParseLikeList();
            }
            public bool NextPage()
            {
                return GetAndParseLikeList();
            }

            private bool GetAndParseLikeList()
            {
                Client.QueryString.Add("variables", String.Format(STR_QUERY_VARIABLE, UserId, INT_RESULT_COUNT, NextCursor));
                Client.QueryString.Add("features", STR_QUERY_FEATURE);

                String likePage = Client.DownloadString(STR_TWITTER_LIKE_URL);

                Client.QueryString.Clear();

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
                        int tweetContentStart = itemString.LastIndexOf("\"legacy\":{\"created_at\"");
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
                        if (medias.Count == 0)
                            continue;
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
                return likeItems.Count == INT_RESULT_COUNT;
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
