using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EbonCorvin
{
    class TimelineFetcher
    {
        // Like Timeline
        // 0: Twitter user ID, 1: Return count, 2: Result cursor
        private const String STR_QUERY_VARIABLE = "%7B%22userId%22%3A{0}%2C%22count%22%3A{1}%2C%22cursor%22%3A{2}%2C%22includePromotedContent%22%3Afalse%2C%22withClientEventToken%22%3Afalse%2C%22withBirdwatchNotes%22%3Afalse%2C%22withVoice%22%3Atrue%2C%22withV2Timeline%22%3Atrue%7D%0A";
        private const String STR_QUERY_FEATURE = "%7B%22rweb_tipjar_consumption_enabled%22%3Atrue%2C%22responsive_web_graphql_exclude_directive_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22creator_subscriptions_tweet_preview_api_enabled%22%3Atrue%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%2C%22responsive_web_graphql_skip_user_profile_image_extensions_enabled%22%3Afalse%2C%22communities_web_enable_tweet_community_results_fetch%22%3Atrue%2C%22c9s_tweet_anatomy_moderator_badge_enabled%22%3Atrue%2C%22articles_preview_enabled%22%3Atrue%2C%22tweetypie_unmention_optimization_enabled%22%3Atrue%2C%22responsive_web_edit_tweet_api_enabled%22%3Atrue%2C%22graphql_is_translatable_rweb_tweet_is_translatable_enabled%22%3Atrue%2C%22view_counts_everywhere_api_enabled%22%3Atrue%2C%22longform_notetweets_consumption_enabled%22%3Atrue%2C%22responsive_web_twitter_article_tweet_consumption_enabled%22%3Atrue%2C%22tweet_awards_web_tipping_enabled%22%3Afalse%2C%22creator_subscriptions_quote_tweet_preview_enabled%22%3Afalse%2C%22freedom_of_speech_not_reach_fetch_enabled%22%3Atrue%2C%22standardized_nudges_misinfo%22%3Atrue%2C%22tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled%22%3Atrue%2C%22rweb_video_timestamps_enabled%22%3Atrue%2C%22longform_notetweets_rich_text_read_enabled%22%3Atrue%2C%22longform_notetweets_inline_media_enabled%22%3Atrue%2C%22responsive_web_enhance_cards_enabled%22%3Afalse%7D";
        private const String STR_QUERY_FIELDTOGGLES = "%7B%22withArticlePlainText%22%3Afalse%7D";

        // Home Latest Timeline (Following) and Home Timeline (For you)
        private const String STR_QUERY_BODY = "{{\"variables\":{{\"count\":{0},\"cursor\":{1},\"includePromotedContent\":true,\"latestControlAvailable\":true,\"requestContext\":\"launch\",\"withCommunity\":true}},\"features\":{{\"rweb_tipjar_consumption_enabled\":true,\"responsive_web_graphql_exclude_directive_enabled\":true,\"verified_phone_label_enabled\":false,\"creator_subscriptions_tweet_preview_api_enabled\":true,\"responsive_web_graphql_timeline_navigation_enabled\":true,\"responsive_web_graphql_skip_user_profile_image_extensions_enabled\":false,\"communities_web_enable_tweet_community_results_fetch\":true,\"c9s_tweet_anatomy_moderator_badge_enabled\":true,\"articles_preview_enabled\":true,\"tweetypie_unmention_optimization_enabled\":true,\"responsive_web_edit_tweet_api_enabled\":true,\"graphql_is_translatable_rweb_tweet_is_translatable_enabled\":true,\"view_counts_everywhere_api_enabled\":true,\"longform_notetweets_consumption_enabled\":true,\"responsive_web_twitter_article_tweet_consumption_enabled\":true,\"tweet_awards_web_tipping_enabled\":false,\"creator_subscriptions_quote_tweet_preview_enabled\":false,\"freedom_of_speech_not_reach_fetch_enabled\":true,\"standardized_nudges_misinfo\":true,\"tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled\":true,\"rweb_video_timestamps_enabled\":true,\"longform_notetweets_rich_text_read_enabled\":true,\"longform_notetweets_inline_media_enabled\":true,\"responsive_web_enhance_cards_enabled\":false}}}}";

        //URLs
        private const String STR_TWITTER_LIKE_URL = "https://x.com/i/api/graphql/ayhH-V7xvuv4nPZpkpuhFA/Likes";
        private const String STR_TWITTER_HOMETL_URL = "https://x.com/i/api/graphql/1u0Wlkw6Ru1NwBUD-pDiww/HomeTimeline";
        private const String STR_TWITTER_HOMELATESTTL_URL = "https://x.com/i/api/graphql/9EwYy8pLBOSFlEoSP2STiQ/HomeLatestTimeline";

        // Authorization header, fixed for everyone
        private const String STR_TWITTER_BEARER_KEY = "Bearer AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";

        // Transaction ID, it's required for the API, missing the header or wrong value will result in failed call
        // Surprisingly, you can just supply a fixed value and the API call would still work
        // See https://antibot.blog/twitter-header-part-3/ for explanation.
        private const String STR_HEADER_TRANS_ID = "EJX1YEiHqmTTm5SfphLMs0d/Kxy9dN7uwYV1U5k51KmBUU4aaraHO1QTvnPF8DSF8pe/CxJuhQBXs53lcLVBhuQMCMxHEw";

        private WebClient Client { get; set; }
        private String UserId { get; set; }
        private String Cookie { get; set; }
        private String CSRT_Token { get; set; }
        private int ResultCount { get; set; } = 20;

        public TimelineFetcher(String twitterId, String cookie, String csrtToken, int resultCount)
        {
            ResultCount = resultCount;
            UserId = twitterId;
            Cookie = cookie;
            CSRT_Token = csrtToken;

            Client = new WebClient();
            Client.Headers.Add("x-csrf-token", csrtToken);
            Client.Headers.Add("Cookie", cookie);
            Client.Headers.Add("Authorization", STR_TWITTER_BEARER_KEY);
            Client.Headers.Add("X-Client-Transaction-Id", STR_HEADER_TRANS_ID);
            Client.Encoding = Encoding.UTF8;
        }

        public String FetchLikeTimeline(String cursor = "null")
        {
            Client.QueryString.Add("variables", String.Format(STR_QUERY_VARIABLE, UserId, ResultCount, cursor));
            Client.QueryString.Add("features", STR_QUERY_FEATURE);
            Client.QueryString.Add("fieldToggles", STR_QUERY_FIELDTOGGLES);

            String likePage = Client.DownloadString(STR_TWITTER_LIKE_URL);

            Client.QueryString.Clear();

            return likePage;
        }

        public String FetchHomeLatestTimeline(String cursor = "null")
        {
            return FetchHomeTimeline(cursor, STR_TWITTER_HOMELATESTTL_URL);
        }

        public String FetchHomeTimeline(String cursor = "null", String url = STR_TWITTER_HOMETL_URL)
        {
            Client.Headers.Add("Content-Type", "application/json");
            return Client.UploadString(url, String.Format(STR_QUERY_BODY, ResultCount, cursor));
        }
    }
}
