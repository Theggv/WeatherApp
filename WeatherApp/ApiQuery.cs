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
        private static string url = "https://api.openweathermap.org/data/2.5/forecast";
        private static string unit = "metric";
        private static string xml = "mode=xml";

        public enum DataType
        {
            Xml,
            Json
        }

        public static string ToQuery(City city)
        {
            return $"{url}?appid={apiKey}&units={unit}&{xml}&id={city.Id}";
        }

        public static string ToQuery(Location loc)
        {
            if (loc.IsSearchByCoords)
                return $"{url}?appid={apiKey}&units={unit}&{xml}&lat={loc.Latitude}&lon={loc.Longtitude}";
            else if(loc.ForecastId != 0)
                return $"{url}?appid={apiKey}&units={unit}&{xml}&id={loc.ForecastId}";
            else if(loc.CountryCode != null)
                return $"{url}?appid={apiKey}&units={unit}&{xml}&q={loc.Name.Replace("'", "")},{loc.CountryCode}";
            else
                return $"{url}?appid={apiKey}&units={unit}&{xml}&q={loc.Name.Replace("'", "")}";
        }
    }
}
