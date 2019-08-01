using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
