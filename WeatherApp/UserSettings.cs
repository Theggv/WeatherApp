using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace WeatherApp
{
    [Serializable]
    public class UserSettings
    {
        public List<LocationInfo> Locations { get; set; }

        public LocationInfo DefaultLocation { get; set; }


        public UserSettings() { }

        public static UserSettings LoadSettings()
        {
            try
            {
                FileStream fs = new FileStream("settings.xml", FileMode.Open);

                var xmlSerializer = new XmlSerializer(typeof(UserSettings));
                var settings = (UserSettings)xmlSerializer.Deserialize(fs);

                fs.Close();

                return settings;
            }
            catch
            {
                return null;
            }
        }

        public void SaveSettings(IEnumerable<BrieflyVM> brieflies, BrieflyVM loadOnStart = null)
        {
            Locations = brieflies.Select(item => new LocationInfo(item.Location)).ToList();
            DefaultLocation = new LocationInfo(loadOnStart.Location);

            FileStream fs = new FileStream("settings.xml", FileMode.Create);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(UserSettings));
            xmlSerializer.Serialize(fs, this);

            fs.Close();
        }


        [Serializable]
        public sealed class LocationInfo
        {
            public int Id { get; set; }

            public int ForecastId { get; set; }

            public string Name { get; set; }

            public string AlternativeName { get; set; }

            public double Latitude { get; set; }

            public double Longtitude { get; set; }

            public bool IsSearchByCoords { get; set; }


            public LocationInfo() { }


            public LocationInfo(Location loc)
            {
                Id = loc.Id;
                ForecastId = loc.ForecastId;
                Name = loc.Name;
                AlternativeName = loc.AlternativeName;
                Latitude = loc.Latitude;
                Longtitude = loc.Longtitude;
                IsSearchByCoords = loc.IsSearchByCoords;
            }

            public Location ToLocation()
            {
                return new Location
                {
                    Id = Id,
                    ForecastId = ForecastId,
                    Name = Name,
                    AlternativeName = AlternativeName,
                    Latitude = Latitude,
                    Longtitude = Longtitude,
                    IsSearchByCoords = IsSearchByCoords
                };
            }
        }
    }
}
