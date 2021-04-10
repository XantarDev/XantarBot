using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Tweetinvi;

namespace XantarBot
{
    class Program
    {
        const char FULL = '▓';
        const char EMPTY = '░';

        const string CSV = "https://raw.githubusercontent.com/datadista/datasets/master/COVID%2019/ccaa_vacunas.csv";

        const string _CONSUMER_KEY = "WRITE HERE CONSUMER_KEY";
        const string _CONSUMER_SECRET = "WRITE HERE _CONSUMER_SECRET";
        const string _ACCESS_TOKEN = "WRITE HERE _ACCESS_TOKEN";
        const string _ACCESS_SECRET = "WRITE HERE _ACCESS_SECRET";
       

        static void Main(string[] args)
        {
            var i= 0;
            if(args.Count() == 4)
                i = 0;
            else
                i = 1;
                
            var CONSUMER_KEY = args.ElementAtOrDefault(i++) ?? _CONSUMER_KEY; 
            var CONSUMER_SECRET = args.ElementAtOrDefault(i++) ?? _CONSUMER_SECRET; 
            var ACCESS_TOKEN = args.ElementAtOrDefault(i++) ?? _ACCESS_TOKEN; 
            var ACCESS_SECRET = args.ElementAtOrDefault(i++) ?? _ACCESS_SECRET; 

            Console.WriteLine($"");
            Console.WriteLine($"CONSUMER_KEY: {CONSUMER_KEY.Substring(0,5)}");
            Console.WriteLine($"CONSUMER_SECRET: {CONSUMER_SECRET.Substring(0,5)}");
            Console.WriteLine($"ACCESS_TOKEN: {ACCESS_TOKEN.Substring(0,5)}");
            Console.WriteLine($"ACCESS_SECRET: {ACCESS_SECRET.Substring(0,5)}");
            Console.WriteLine($"");
            
            var client = new TwitterClient(CONSUMER_KEY, CONSUMER_SECRET, ACCESS_TOKEN, ACCESS_SECRET);
            var user = client.Users.GetAuthenticatedUserAsync().Result;

            var lastUpdate = Regex.Match(user.GetHomeTimelineAsync().Result.FirstOrDefault().Text, @"\[(.*?)\]").Groups[1].Value;
            var vaccination = GetVaccination();
            
            //if (Convert.ToDateTime(lastUpdate, new CultureInfo("es-ES")) < vaccination.Date)
            //{
                var progressBar = new string(FULL, (int)vaccination.Percentage / 5) + new string(EMPTY, 20 - ((int)vaccination.Percentage / 5));
                var textTweet = $"[{vaccination.Date.ToString("dd/MM/yyyy")}] {vaccination.Percentage}%\n{progressBar}";
                _ = user.PublishTweetAsync(textTweet).Result;

                Console.WriteLine($"Tweet published: {textTweet}");
            //}
            //else
            //{
            //    Console.WriteLine($"No updated data. Try again later.");
            //}
        }

        private static Vaccination GetVaccination()
        {
            using (var client = new WebClient())
            {
                string data = client.DownloadString(CSV);

                using (var csvReader = new CsvReader(new StringReader(data), new CsvConfiguration(new CultureInfo("es-ES")) { Delimiter = "," }))
                    return csvReader.GetRecords<Vaccination>().Where(v => v.Area == "España").OrderByDescending(v => v.Date).First();
            }
        }
    }

    public class Vaccination
    {
        [Name("Fecha publicación")]
        public DateTime Date { get; set; }

        [Name("CCAA")]
        public string Area { get; set; }

        [Name("Porcentaje con pauta completa")]
        public decimal Percentage { get; set; }
    }
}
