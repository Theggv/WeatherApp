using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WeatherApp
{
    /*
geonameid         : integer id of record in geonames database
name              : name of geographical point (utf8) varchar(200)
asciiname         : name of geographical point in plain ascii characters, varchar(200)
alternatenames    : alternatenames, comma separated, ascii names automatically transliterated, convenience attribute from alternatename table, varchar(10000)
              : latitude in decimal degrees (wgs84)
longitude         : longitude in decimal degrees (wgs84)
feature class     : see http://www.geonames.org/export/codes.html, char(1)
feature code      : see http://www.geonames.org/export/codes.html, varchar(10)
country code      : ISO-3166 2-letter country code, 2 characters
cc2               : alternate country codes, comma separated, ISO-3166 2-letter country code, 200 characters
admin1 code       : fipscode (subject to change to iso code), see exceptions below, see file admin1Codes.txt for display names of this code; varchar(20)
admin2 code       : code for the second administrative division, a county in the US, see file admin2Codes.txt; varchar(80) 
admin3 code       : code for third level administrative division, varchar(20)
admin4 code       : code for fourth level administrative division, varchar(20)
population        : bigint (8 byte int) 
elevation         : in meters, integer
dem               : digital elevation model, srtm3 or gtopo30, average elevation of 3''x3'' (ca 90mx90m) or 30''x30'' (ca 900mx900m) area in meters, integer. srtm processed by cgiar/ciat.
timezone          : the iana timezone id (see file timeZone.txt) varchar(40)
modification date : date of last modification in yyyy-MM-dd format
     */
    public class Location
    {
        public int Id { get; set; }
        public int ForecastId { get; set; }
        public string Name { get; set; }
        public string AlternativeName { get; set; }
        public double Latitude { get; set; }
        public double Longtitude { get; set; }
        public string FeatureClass { get; set; }
        public string FeatureCode { get; set; }
        public string CountryCode { get; set; }
        public string AlternativeCode { get; set; }
        public string AdminCode1 { get; set; }
        public string AdminCode2 { get; set; }
        public string AdminCode3 { get; set; }
        public string AdminCode4 { get; set; }
        public long Population { get; set; }
        public int Elevation { get; set; }
        public string TimeZone { get; set; }

        public bool IsSearchByCoords { get; set; } = false;

        public string DisplayName => AlternativeName != "" ? AlternativeName : Name;

        public Location() { }

        public Location(params string[] args)
        {
            Id = int.Parse(args[0]);
            Name = args[1];
            Latitude = FixDouble.ToDouble(args[4]);
            Longtitude = FixDouble.ToDouble(args[5]);
            FeatureClass = args[6];
            FeatureCode = args[7];
            CountryCode = args[8];
            AlternativeCode = args[9];
            AdminCode1 = args[10];
            AdminCode2 = args[11];
            AdminCode3 = args[12];
            AdminCode4 = args[13];
            Population = (long)FixDouble.ToDouble(args[14]);
            Elevation = (int)FixDouble.ToDouble(args[15]);
            TimeZone = args[16];
        }

        public Location(params object[] args)
        {
            Id = int.Parse(args[0].ToString());
            ForecastId = int.Parse(args[1].ToString());

            Name = args[2].ToString();
            AlternativeName = args[3].ToString();

            Latitude = FixDouble.ToDouble(args[4].ToString());
            Longtitude = FixDouble.ToDouble(args[5].ToString());

            FeatureClass = args[6].ToString();
            FeatureCode = args[7].ToString();
            CountryCode = args[8].ToString();

            AdminCode1 = args[9].ToString();
            AdminCode2 = args[10].ToString();
            AdminCode3 = args[11].ToString();
            AdminCode4 = args[12].ToString();

            Population = (long)FixDouble.ToDouble(args[13].ToString());
        }

        public string ToQuery()
        {
            return $"({Format(ForecastId)}, " +
                $"{Format(Name)}, {GetRussianName()}, " +
                $"{Format(Latitude)}, {Format(Longtitude)}, {Format(FeatureClass)}, {Format(FeatureCode)}, " +
                $"{Format(CountryCode)}, " +
                $"{Format(AdminCode1)}, {Format(AdminCode2)}, {Format(AdminCode3)}, {Format(AdminCode4)}, " +
                $"{Format(Population)}),";
        }

        private string Format(string str)
        {
            if (str == null || str == "")
                return "null";

            return $"'{str.Replace("'", "''")}'";
        }

        private string Format(double d)
        {
            var str = d.ToString().Replace(',', '.');

            if (str == null || str == "")
                return "null";

            return str;
        }

        private string Format(char c) => Format("" + c);

        private string GetRussianName()
        {
            var names = AlternativeName?.Split(',');

            if (names == null || names.Length == 0)
                return Format(null);

            var alphabet = Enumerable.Range('а', 33).ToList();
            string name = "";

            for (int i = 0; i < names.Length; i++)
            {
                name = names[i].ToLowerInvariant();

                if (name == "")
                    continue;

                for (int j = 0; j < alphabet.Count; j++)
                {
                    if (name.Contains((char)alphabet[j]))
                        return Format(names[i]);
                }
            }

            return Format(null);
        }
    }

    public class AlternativeName
    {
        public int GeoNameId { get; set; }

        public string Iso { get; set; }

        public string Name { get; set; }


        public AlternativeName() { }

        public AlternativeName(params string[] args)
        {
            GeoNameId = int.Parse(args[1]);
            Iso = args[2];
            Name = args[3];
        }
    }
}
