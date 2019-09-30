using System;
using System.Xml;
using System.ServiceModel.Syndication;

namespace RSSFetcher
{
    public class Jobs
    {
        public static void Fetch(string jobFunctionId, string jobCategory)
        {
            using (RSSDatabase rssDb = new RSSDatabase())
            {
                try
                {
                    string url = "https://hk.jobsdb.com/HK/en/Rss/JobListing?jobFunctionId=" + jobFunctionId;
                    using (XmlReader reader = XmlReader.Create(url))
                    {
                        SyndicationFeed feed = SyndicationFeed.Load(reader);

                        foreach (SyndicationItem item in feed.Items)
                        {
                            RSSRecord record = new RSSRecord()
                            {
                                ID = item.Id,
                                Subject = item.Title.Text,
                                Summary = item.Summary.Text.Replace("<html><body>", "").Replace("</body></html>", ""),
                                Links = item.Links[0].Uri.OriginalString,
                                PublishDate = item.PublishDate.LocalDateTime
                            };
                            rssDb.InsertRSSRecord(record);
                        }
                    }
                }
                catch (System.Net.WebException ex)
                {
                    Console.WriteLine("Error: " + ex.StackTrace);
                }
            }
        }
    }
}
