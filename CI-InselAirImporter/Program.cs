using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CI_InselAirImporter
{
    class Program
    {
        static void Main(string[] args)
        {
            string addressUrl = "https://www.fly-inselair.com/en/timetable/?from=&to=&directOnly=true&timetable=search";
            const string referer = "https://www.fly-inselair.com/en/timetable/";
            const string ua = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
            string page = string.Empty;
            CultureInfo ci = new CultureInfo("en-US");
            List<CIFLight> CIFLights = new List<CIFLight> { };
            Regex rgxIATAAirport = new Regex(@"[A-Z]{3}");

            string TEMP_FromIATA = null;
            string TEMP_ToIATA = null;
            DateTime TEMP_ValidFrom = new DateTime();
            DateTime TEMP_ValidTo = new DateTime();
            int TEMP_Conversie = 0;
            Boolean TEMP_FlightMonday = false;
            Boolean TEMP_FlightTuesday = false;
            Boolean TEMP_FlightWednesday = false;
            Boolean TEMP_FlightThursday = false;
            Boolean TEMP_FlightFriday = false;
            Boolean TEMP_FlightSaterday = false;
            Boolean TEMP_FlightSunday = false;
            DateTime TEMP_DepartTime = new DateTime();
            DateTime TEMP_ArrivalTime = new DateTime();
            Boolean TEMP_FlightCodeShare = false;
            string TEMP_FlightNumber = null;
            string TEMP_Aircraftcode = null;
            TimeSpan TEMP_DurationTime = TimeSpan.MinValue;
            Boolean TEMP_FlightNextDayArrival = false;
            int TEMP_FlightNextDays = 0;

            using (WebClient client = new WebClient())
            {
                client.Headers.Add("user-agent", ua);
                client.Headers.Add("Referer", referer);
                client.Proxy = null;

                page = client.DownloadString(addressUrl);

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(page);

                HtmlAgilityPack.HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//table[@id='timetable']/tbody/tr");
                foreach (var node in nodes)
                {
                    TEMP_FlightNumber = node.SelectSingleNode("//td[@class='flight']").InnerText;
                    MatchCollection matches = rgxIATAAirport.Matches(node.SelectSingleNode("//td[@class='citypair']").InnerText);

                    TEMP_FromIATA = matches[0].Value;
                    TEMP_ToIATA = matches[1].Value;
                    
                    TEMP_DepartTime = DateTime.Parse(node.SelectSingleNode("//td[@class='departure']").InnerText);
                    TEMP_ArrivalTime = DateTime.Parse(node.SelectSingleNode("//td[@class='arrival']").InnerText);
                    TimeSpan ts = TEMP_ArrivalTime - TEMP_DepartTime;
                    TEMP_DurationTime = ts;
                    //node.SelectSingleNode("//td[@class='duration']");
                    if (!String.IsNullOrEmpty(node.SelectSingleNode("//td[@class='mon']").InnerText)) { TEMP_FlightMonday = true; }
                    if (!String.IsNullOrEmpty(node.SelectSingleNode("//td[@class='tue']").InnerText)) { TEMP_FlightTuesday = true; }
                    if (!String.IsNullOrEmpty(node.SelectSingleNode("//td[@class='wed']").InnerText)) { TEMP_FlightWednesday = true; }
                    if (!String.IsNullOrEmpty(node.SelectSingleNode("//td[@class='thu']").InnerText)) { TEMP_FlightThursday = true; }
                    if (!String.IsNullOrEmpty(node.SelectSingleNode("//td[@class='fri']").InnerText)) { TEMP_FlightFriday = true; }
                    if (!String.IsNullOrEmpty(node.SelectSingleNode("//td[@class='sat']").InnerText)) { TEMP_FlightSaterday = true; }
                    if (!String.IsNullOrEmpty(node.SelectSingleNode("//td[@class='sun']").InnerText)) { TEMP_FlightSunday = true; }
                    //Console.WriteLine(node.InnerHtml.ToString());

                    CIFLights.Add(new CIFLight
                    {
                        FromIATA = TEMP_FromIATA,
                        ToIATA = TEMP_ToIATA,
                        FromDate = TEMP_ValidFrom,
                        ToDate = TEMP_ValidTo,
                        ArrivalTime = TEMP_ArrivalTime,
                        DepartTime = TEMP_DepartTime,
                        FlightAircraft = TEMP_Aircraftcode,
                        FlightAirline = "InselAir",
                        FlightMonday = TEMP_FlightMonday,
                        FlightTuesday = TEMP_FlightTuesday,
                        FlightWednesday = TEMP_FlightWednesday,
                        FlightThursday = TEMP_FlightThursday,
                        FlightFriday = TEMP_FlightFriday,
                        FlightSaterday = TEMP_FlightSaterday,
                        FlightSunday = TEMP_FlightSunday,
                        FlightNumber = TEMP_FlightNumber,
                        FlightOperator = null,
                        FlightDuration = TEMP_DurationTime.ToString(),
                        FlightCodeShare = false,
                        FlightNextDayArrival = TEMP_FlightNextDayArrival,
                        FlightNextDays = TEMP_FlightNextDays
                    });
                    // Cleaning All but From and To 
                    TEMP_ValidFrom = new DateTime();
                    TEMP_ValidTo = new DateTime();
                    TEMP_Conversie = 0;
                    TEMP_FlightMonday = false;
                    TEMP_FlightTuesday = false;
                    TEMP_FlightWednesday = false;
                    TEMP_FlightThursday = false;
                    TEMP_FlightFriday = false;
                    TEMP_FlightSaterday = false;
                    TEMP_FlightSunday = false;
                    TEMP_DepartTime = new DateTime();
                    TEMP_ArrivalTime = new DateTime();
                    TEMP_FlightNumber = null;
                    TEMP_Aircraftcode = null;
                    TEMP_DurationTime = TimeSpan.MinValue;
                    TEMP_FlightCodeShare = false;
                    TEMP_FlightNextDayArrival = false;
                    TEMP_FlightNextDays = 0;
                }

            }
            System.Xml.Serialization.XmlSerializer writer =
            new System.Xml.Serialization.XmlSerializer(CIFLights.GetType());
            string myDir = AppDomain.CurrentDomain.BaseDirectory + "\\output";
            Directory.CreateDirectory(myDir);
            StreamWriter file =
               new System.IO.StreamWriter("output\\output.xml");

            writer.Serialize(file, CIFLights);
            file.Close();

        }
    }

    public class CIFLight
    {
        // Auto-implemented properties. 
        public string FromIATA;
        public string ToIATA;
        public DateTime FromDate;
        public DateTime ToDate;
        public Boolean FlightMonday;
        public Boolean FlightTuesday;
        public Boolean FlightWednesday;
        public Boolean FlightThursday;
        public Boolean FlightFriday;
        public Boolean FlightSaterday;
        public Boolean FlightSunday;
        public DateTime DepartTime;
        public DateTime ArrivalTime;
        public String FlightNumber;
        public String FlightAirline;
        public String FlightOperator;
        public String FlightAircraft;
        public String FlightDuration;
        public Boolean FlightCodeShare;
        public Boolean FlightNextDayArrival;
        public int FlightNextDays;
    }
}
