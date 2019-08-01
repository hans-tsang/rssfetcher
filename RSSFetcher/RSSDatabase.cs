using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSSFetcher
{
    class RSSRecord
    {
        public string ID { get; set; }

        public string Subject { get; set; }

        public string Summary { get; set; }

        public string Links { get; set; }

        public DateTime PublishDate { get; set; }

        public string JobCategory { get; set; }
    }

    class RSSDatabase : IDisposable
    {
        private SQLiteConnection m_dbConnection;

        public RSSDatabase()
        {
            // Prepare the database
            const string sampleDbPath = "sampledb/rss.db";
            const string dbPath = "./rss.db";

            if (!File.Exists(dbPath))
            {
                File.Copy(sampleDbPath, dbPath);
            }

            m_dbConnection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            m_dbConnection.Open();
        }

        public void Dispose()
        {
            m_dbConnection.Close();
        }

        public void InsertRSSRecord(RSSRecord record)
        {
            try
            {
                string sql = "insert or replace into JOBS (ID, SUBJECT, SUMMARY, LINK, PUBLISHDATE, CATEGORY) values (?,?,?,?,?,?)";
                var command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.AddWithValue("ID", record.ID);
                command.Parameters.AddWithValue("subject", record.Subject);
                command.Parameters.AddWithValue("summary", record.Summary);
                command.Parameters.AddWithValue("links", record.Links);
                command.Parameters.AddWithValue("publishDate", record.PublishDate);
                command.Parameters.AddWithValue("category", record.JobCategory);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("InsertRSSRecord Error: " + ex.StackTrace);
            }
        }
    }
}
