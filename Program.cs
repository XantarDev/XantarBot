using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
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

        const string CONSUMER_KEY = "WRITE HERE CONSUMER_KEY";
        const string CONSUMER_SECRET = "WRITE HERE CONSUMER_SECRET";
        const string ACCESS_TOKEN = "WRITE HERE ACCESS_TOKEN";
        const string ACCESS_SECRET = "WRITE HERE ACCESS_SECRET";

        static void Main(string[] args)
        {
            int i = args.Count() == 4 ? 0 : 1;
            string consumerKey = args.ElementAtOrDefault(i++) ?? CONSUMER_KEY;
            string consumerSecret = args.ElementAtOrDefault(i++) ?? CONSUMER_SECRET;
            string accessToken = args.ElementAtOrDefault(i++) ?? ACCESS_TOKEN;
            string accessSecret = args.ElementAtOrDefault(i++) ?? ACCESS_SECRET;

            var client = new TwitterClient(consumerKey, consumerSecret, accessToken, accessSecret);
            var user = client.Users.GetAuthenticatedUserAsync().Result;

            DateTime lastUpdate = Convert.ToDateTime(Regex.Match(user.GetUserTimelineAsync().Result.FirstOrDefault().Text, @"\[(.*?)\]").Groups[1].Value, new CultureInfo("es-ES"));
            List<Vaccination> vaccinations = GetVaccinations(lastUpdate);

            if (vaccinations.Any())
                foreach (var vaccination in vaccinations.OrderBy(v => v.Date))
                {
                    var progressBar = new string(FULL, (int)vaccination.Percentage / 5) + new string(EMPTY, 20 - ((int)vaccination.Percentage / 5));
                    var tweet = $"[{vaccination.Date.ToString("dd/MM/yyyy")}] {vaccination.Percentage}% #COVID19\n{progressBar}";
                    _ = user.PublishTweetAsync(tweet).Result;

                    Console.WriteLine($"Tweet published: {tweet}");
                }
            else Console.WriteLine($"No updated data. Try again later.");
        }

        private static List<Vaccination> GetVaccinations(DateTime lastUpdate)
        {
            using (var client = new WebClient())
            {
                string data = client.DownloadString(CSV);

                using (var csvReader = new CsvReader(new StringReader(data), new CsvConfiguration(new CultureInfo("es-ES")) { Delimiter = "," }))
                    return csvReader.GetRecords<Vaccination>().Where(v => v.Area == "España" && v.Date > lastUpdate).OrderByDescending(v => v.Date).ToList();
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