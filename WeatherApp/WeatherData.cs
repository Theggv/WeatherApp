using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Windows;

namespace WeatherApp
{
    public class WeatherData
    {
        public City Location { get; set; }

        public List<Forecast> Forecasts { get; set; }

        public WeatherData()
        {
            Location = new City();
            Forecasts = new List<Forecast>();
        }

        public WeatherData(string xml)
        {
            Location = new City();
            Forecasts = new List<Forecast>();

            XmlDocument doc = new XmlDocument();
            
            doc.Load(xml);
            Location = new City(doc);
            GetForecast(doc);
        }

        public static Task<WeatherData> FromXMLAsync(string xml)
        {
            return Task.Run(() => {
                return new WeatherData(xml);
            });
        }

        private void GetForecast(XmlDocument doc)
        {
            string forecastListQuery = "weatherdata/forecast/*";

            var forecasts = doc.SelectNodes(forecastListQuery);

            foreach (XmlNode forecastNode in forecasts)
            {
                var forecast = new Forecast(forecastNode);
                
                Forecasts.Add(forecast);
            }
        }
    }
}
