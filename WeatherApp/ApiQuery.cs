using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherApp
{
    public static class ApiQuery
    {
        private static string apiKey = "8ede6d94b239ab8dedafe9270a2d0d36";

        public enum DataType
        {
            Xml,
            Json
        }

        public static string ToQuery(City city, DataType dataType = DataType.Xml)
        {
            string isXml = dataType == DataType.Xml ? "&mode=xml" : "";

            return $"https://api.openweathermap.org/data/2.5/forecast?id={city.Id}&appid={apiKey}&units=metric{isXml}";
        }

        public static string ToQuery(Location loc, DataType dataType = DataType.Xml)
        {
            string isXml = dataType == DataType.Xml ? "&mode=xml" : "";

            if (loc.IsSearchByCoords)
                return $"https://api.openweathermap.org/data/2.5/forecast?lat={loc.Latitude}&lon={loc.Longtitude}&appid={apiKey}&units=metric{isXml}";
            else
                return $"https://api.openweathermap.org/data/2.5/forecast?q={loc.Name.Replace("'", "")},{loc.CountryCode}&appid={apiKey}&units=metric{isXml}";
        }
    }
}
