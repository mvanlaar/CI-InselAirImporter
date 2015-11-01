using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

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
            // There is no valid from and to date information in the website. So Get the current date and add 30 days.
            DateTime TEMP_ValidFrom = DateTime.Today;
            DateTime TEMP_ValidTo = TEMP_ValidFrom.AddDays(30);
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
                Console.WriteLine("Downloading HTML TimeTable from the InselAir Website...");
                client.Headers.Add("user-agent", ua);
                client.Headers.Add("Referer", referer);
                client.Proxy = null;

                page = client.DownloadString(addressUrl);

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(page);

                HtmlAgilityPack.HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//table[@id='timetable']/tbody/tr");
                foreach (var node in nodes)
                {
                    HtmlNode Node_Header = node.SelectSingleNode("td[@class='flight-citypair']");
                    if (Node_Header == null)
                    {
                        TEMP_FlightNumber = node.SelectSingleNode("td[@class='flight']").InnerText;                    

                        MatchCollection matches = rgxIATAAirport.Matches(node.SelectSingleNode("td[@class='citypair']").InnerText);

                        TEMP_FromIATA = matches[0].Value;
                        TEMP_ToIATA = matches[1].Value;

                        TEMP_DepartTime = DateTime.Parse(node.SelectSingleNode("td[@class='departure']").InnerText);
                        TEMP_ArrivalTime = DateTime.Parse(node.SelectSingleNode("td[@class='arrival']").InnerText);
                        TimeSpan ts = TEMP_ArrivalTime - TEMP_DepartTime;
                        TEMP_DurationTime = ts;
                        //node.SelectSingleNode("//td[@class='duration']");
                        HtmlNode Node_Monday = node.SelectSingleNode("td[@class='mon']/span");
                        if (Node_Monday != null) { TEMP_FlightMonday = true; }
                        HtmlNode Node_Tuesday = node.SelectSingleNode("td[@class='tue']/span");
                        if (Node_Tuesday != null) { TEMP_FlightTuesday = true; }
                        HtmlNode Node_Wednesday = node.SelectSingleNode("td[@class='wed']/span");
                        if (Node_Wednesday != null) { TEMP_FlightWednesday = true; }
                        HtmlNode Node_Thursday = node.SelectSingleNode("td[@class='thu']/span");
                        if (Node_Thursday != null) { TEMP_FlightThursday = true; }
                        HtmlNode Node_Friday = node.SelectSingleNode("td[@class='fri']/span");
                        if (Node_Friday != null) { TEMP_FlightFriday = true; }
                        HtmlNode Node_Saterday = node.SelectSingleNode("td[@class='sat']/span");
                        if (Node_Saterday != null) { TEMP_FlightMonday = true; }
                        HtmlNode Node_Sunday = node.SelectSingleNode("td[@class='sun']/span");
                        if (Node_Sunday != null) { TEMP_FlightMonday = true; }
                        /*
                        if (!String.IsNullOrEmpty(node.SelectSingleNode("td[@class='tue']").InnerText.Trim())) { TEMP_FlightTuesday = true; }
                        if (!String.IsNullOrEmpty(node.SelectSingleNode("td[@class='wed']").InnerText.Trim())) { TEMP_FlightWednesday = true; }
                        if (!String.IsNullOrEmpty(node.SelectSingleNode("td[@class='thu']").InnerText.Trim())) { TEMP_FlightThursday = true; }
                        if (!String.IsNullOrEmpty(node.SelectSingleNode("td[@class='fri']").InnerText.Trim())) { TEMP_FlightFriday = true; }
                        if (!String.IsNullOrEmpty(node.SelectSingleNode("td[@class='sat']").InnerText.Trim())) { TEMP_FlightSaterday = true; }
                        if (!String.IsNullOrEmpty(node.SelectSingleNode("td[@class='sun']").InnerText.Trim())) { TEMP_FlightSunday = true; }
                        Console.WriteLine(node.SelectSingleNode("//td[@class='mon']").InnerText);
                        */
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
                            FlightOperator = TEMP_FlightNumber.Substring(0, 2),
                            FlightDuration = TEMP_DurationTime.ToString(),
                            FlightCodeShare = false,
                            FlightNextDayArrival = TEMP_FlightNextDayArrival,
                            FlightNextDays = TEMP_FlightNextDays
                        });
                        // Cleaning All but From and To 
                        TEMP_FromIATA = null;
                        TEMP_ToIATA = null;
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
                        Node_Header = null;
                        Node_Monday = null;
                        Node_Tuesday = null;
                        Node_Wednesday = null;
                        Node_Thursday = null;
                        Node_Friday = null;
                        Node_Saterday = null;
                        Node_Sunday = null;
                    }
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

            Console.WriteLine("Insert into Database...");
            for (int i = 0; i < CIFLights.Count; i++) // Loop through List with for)
            {
                using (SqlConnection connection = new SqlConnection("Server=(local);Database=CI-Import;Trusted_Connection=True;"))
                {
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Connection = connection;            // <== lacking
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "InsertFlight";
                        command.Parameters.Add(new SqlParameter("@FlightSource", 7));
                        command.Parameters.Add(new SqlParameter("@FromIATA", CIFLights[i].FromIATA));
                        command.Parameters.Add(new SqlParameter("@ToIATA", CIFLights[i].ToIATA));
                        command.Parameters.Add(new SqlParameter("@FromDate", CIFLights[i].FromDate));
                        command.Parameters.Add(new SqlParameter("@ToDate", CIFLights[i].ToDate));
                        command.Parameters.Add(new SqlParameter("@FlightMonday", CIFLights[i].FlightMonday));
                        command.Parameters.Add(new SqlParameter("@FlightTuesday", CIFLights[i].FlightTuesday));
                        command.Parameters.Add(new SqlParameter("@FlightWednesday", CIFLights[i].FlightWednesday));
                        command.Parameters.Add(new SqlParameter("@FlightThursday", CIFLights[i].FlightThursday));
                        command.Parameters.Add(new SqlParameter("@FlightFriday", CIFLights[i].FlightFriday));
                        command.Parameters.Add(new SqlParameter("@FlightSaterday", CIFLights[i].FlightSaterday));
                        command.Parameters.Add(new SqlParameter("@FlightSunday", CIFLights[i].FlightSunday));
                        command.Parameters.Add(new SqlParameter("@DepartTime", CIFLights[i].DepartTime));
                        command.Parameters.Add(new SqlParameter("@ArrivalTime", CIFLights[i].ArrivalTime));
                        command.Parameters.Add(new SqlParameter("@FlightNumber", CIFLights[i].FlightNumber));
                        command.Parameters.Add(new SqlParameter("@FlightAirline", CIFLights[i].FlightAirline));
                        command.Parameters.Add(new SqlParameter("@FlightOperator", CIFLights[i].FlightOperator));
                        command.Parameters.Add(new SqlParameter("@FlightAircraft", CIFLights[i].FlightAircraft));
                        command.Parameters.Add(new SqlParameter("@FlightCodeShare", CIFLights[i].FlightCodeShare));
                        command.Parameters.Add(new SqlParameter("@FlightNextDayArrival", CIFLights[i].FlightNextDayArrival));
                        command.Parameters.Add(new SqlParameter("@FlightDuration", CIFLights[i].FlightDuration));
                        command.Parameters.Add(new SqlParameter("@FlightNextDays", CIFLights[i].FlightNextDays));
                        command.Parameters.Add(new SqlParameter("@FlightNonStop", "True"));
                        command.Parameters.Add(new SqlParameter("@FlightVia", DBNull.Value));

                        foreach (SqlParameter parameter in command.Parameters)
                        {
                            if (parameter.Value == null)
                            {
                                parameter.Value = DBNull.Value;
                            }
                        }


                        try
                        {
                            connection.Open();
                            int recordsAffected = command.ExecuteNonQuery();
                        }

                        finally
                        {
                            connection.Close();
                        }
                    }
                }

            }

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
