
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using EbonCorvin;
using EbonCorvin.TiwtterTimelineParser;
using System.Linq;

namespace Twitter_Download
{
    public class Program
    {
        private static WebClient clientDl = null;
        public static void Main()
        {
            try
            {
                clientDl = new WebClient();
                ConfigLoader config = new ConfigLoader("config.txt");
                TwitterLikeParser parser = new TwitterLikeParser(TimelineType.LikeTimeline, config["twitter_id"], config["cookie"], config["csrt"], 50);
                while (parser.NextPage())
                {
                    foreach (Tweet tweet in parser.Tweets)
                    {
                        foreach(Media media in tweet.Medias)
                        {
                            DownloadMedia(media, config["path"]);
                        }
                    }
                    if (parser.Tweets.Length == 0)
                    {
                        Console.WriteLine("----------");
                        break;
                    }
                        
                    Console.WriteLine("Last Tweet is on {0}", parser.Tweets.Last().CreateDate);
                    Console.WriteLine("Next Cursor: {0}", parser.NextCursor);
                    Console.WriteLine("----------");
                }
                Console.WriteLine("Done traveling the whole liked post list");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.Read();
        }

        public static void DownloadMedia(Media media, String DownloadPath)
        {
            String url = media.Url;
            try
            {
                String fileName = GetPureFileName(url);
                String filePath = DownloadPath + "\\" + fileName;
                if (File.Exists(filePath))
                    return;
                Console.WriteLine(fileName);
                if (media.MediaType == "image")
                    url += ":orig";
                clientDl.DownloadFile(url, filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to download this file: {0}", url);
                Console.WriteLine(ex.Message);
            }
        }


        private static String GetPureFileName(String url)
        {
            int lastSlash = url.LastIndexOf("/");
            int lastQuery = url.LastIndexOf("?");
            return lastQuery > -1 ?
                url.Substring(lastSlash + 1, lastQuery - lastSlash - 1) :
                url.Substring(lastSlash + 1);

        }
    }
}