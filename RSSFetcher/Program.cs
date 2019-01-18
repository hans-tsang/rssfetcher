using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Data.SQLite;

namespace RSSFetcher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string url = "https://hk.jobsdb.com/HK/en/Rss/JobListing?jobFunctionId=131";
            XmlReader reader = XmlReader.Create(url);
            SyndicationFeed feed = SyndicationFeed.Load(reader);
            reader.Close();
            foreach (SyndicationItem item in feed.Items)
            {
                String ID = item.Id;
                String subject = item.Title.Text;
                String summary = item.Summary.Text;
                String links = item.Links[0].Uri.OriginalString;
                DateTime publishDate = item.PublishDate.LocalDateTime;
                //Console.WriteLine(ID);
                //Console.WriteLine(subject);
                //Console.WriteLine(summary);
                //Console.WriteLine(links);
                //Console.WriteLine(publishDate);

                SQLiteConnection m_dbConnection;
                m_dbConnection = new SQLiteConnection("Data Source=./rss.db;Version=3;");
                m_dbConnection.Open();
                string sql = "insert or replace into JOBS (ID, SUBJECT, SUMMARY, LINK, PUBLISHDATE) values (?,?,?,?,?)";
                var command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.AddWithValue("ID", ID);
                command.Parameters.AddWithValue("subject", subject);
                command.Parameters.AddWithValue("summary", summary);
                command.Parameters.AddWithValue("links", links);
                command.Parameters.AddWithValue("publishDate", publishDate);
                command.ExecuteNonQuery();
            }
            //while (true)
            //{
            //    Console.ReadKey();
            //}
            //Console.ReadKey();
        }
    }
}
