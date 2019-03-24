using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Xml;

namespace WeatherApp
{
    public class City
    {
        public int Id { get; set; }
        
        public string Name { get; set; }
        
        public string Country { get; set; }

        public City() { }

        public City(XmlDocument doc)
        {
            string locPath = "weatherdata/location";

            string nameQuery = $"{locPath}/name";
            string idQuery = $"{locPath}/location/@geobaseid";
            string countryQuery = $"{locPath}/country";

            Name = doc.SelectSingleNode(nameQuery).InnerText;
            Id = int.Parse(doc.SelectSingleNode(idQuery).InnerText);
            Country = doc.SelectSingleNode(countryQuery).InnerText;
        }
    }
}
