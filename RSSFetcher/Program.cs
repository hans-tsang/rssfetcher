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
            Console.WriteLine("Fetching Jobs....");
            Jobs.Fetch("2", "Accounting");
            Jobs.Fetch("15", "Admin & HR");
            Jobs.Fetch("37", "Banking / Finance");
            Jobs.Fetch("113", "Beauty Care / Health");
            Jobs.Fetch("55", "Building & Construction");
            Jobs.Fetch("70", "Design");
            Jobs.Fetch("300", "E-commerce");
            Jobs.Fetch("80", "Education");
            Jobs.Fetch("89", "Engineering");
            Jobs.Fetch("118", "Hospitality / F & B");
            Jobs.Fetch("131", "Information Technology(IT)");
            Jobs.Fetch("151", "Insurance");
            Jobs.Fetch("284", "Management");
            Jobs.Fetch("169", "Manufacturing");
            Jobs.Fetch("175", "Marketing / Public Relations");
            Jobs.Fetch("22", "Media & Advertising");
            Jobs.Fetch("193", "Medical Services");
            Jobs.Fetch("201", "Merchandising & Purchasing");
            Jobs.Fetch("285", "Professional Services");
            Jobs.Fetch("226", "Property / Real Estate");
            Jobs.Fetch("282", "Public / Civil");
            Jobs.Fetch("233", "Sales, CS & Business Devpt");
            Jobs.Fetch("283", "Sciences, Lab, R&D");
            Jobs.Fetch("265", "Transportation & Logistics");
            Jobs.Fetch("272", "Others");
            Console.WriteLine("Done");
        }
    }
}
