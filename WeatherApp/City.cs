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

        public City(int cityId)
        {
            string cityQuery = "SELECT [external_id], [city].[name], [country].[name] " +
                "FROM[dbo].[city] " +
                "JOIN[dbo].[country] " +
                "ON country.code = country_id " +
                $"WHERE [external_id] = {cityId};";

            using (SqlConnection conn = new SqlConnection(ConnectionInfo.ConnString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand
                {
                    Connection = conn,
                    CommandText = cityQuery
                };

                SqlDataReader reader = command.ExecuteReader();

                if (!reader.HasRows)
                {
                    reader.Close();
                    return;
                }

                reader.Read();

                Id = int.Parse(reader[0].ToString());
                Name = reader[1].ToString();
                Country = reader[2].ToString();

                reader.Close();
            }  
        }
    }
}
