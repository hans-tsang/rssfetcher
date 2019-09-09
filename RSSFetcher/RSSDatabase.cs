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

    /// <summary>
    /// RSSDatabase Operation, the database named rss.db will be created at the same location of the binary
    /// </summary>
    class RSSDatabase : IDisposable
    {
        private SQLiteConnection m_dbConnection;

        public RSSDatabase()
        {
            InitDatabase();
        }

        /// <summary>
        /// Set up the database and required table
        /// </summary>
        private void InitDatabase()
        {
            // Prepare the database
            const string dbPath = "./rss.db";

            m_dbConnection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            m_dbConnection.Open();

            try
            {
                var command = m_dbConnection.CreateCommand();
                command.CommandText = @"CREATE TABLE IF NOT EXISTS JOBS (
    `ID`    TEXT NOT NULL,
    `SUBJECT`   TEXT,
    `SUMMARY`   TEXT,
    `LINK`  TEXT,
    `PUBLISHDATE`   TEXT, CATEGORY TEXT,
    PRIMARY KEY(`ID`)
);";
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitDatabase Error: " + ex.StackTrace);
            }
        }

        public void Dispose()
        {
            m_dbConnection.Close();
        }

        /// <summary>
        /// Insert RSS feed into table JOBS
        /// </summary>
        /// <param name="record">RSS Feed Record</param>
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
