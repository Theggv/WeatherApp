using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WeatherApp
{
    public class Forecast
    {
        public int Id { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public Wind Wind { get; set; }

        public Temperature Temperature { get; set; }

        public double Humidity { get; set; }

        public Pressure Pressure { get; set; }

        public Symbol Symbol { get; set; }

        public Precipitation Precipitation { get; set; }

        public Clouds Clouds { get; set; }

        public Forecast()
        {
            From = new DateTime();
            To = new DateTime();
            Wind = new Wind();
            Temperature = new Temperature();
            Pressure = new Pressure();
            Symbol = new Symbol();
            Precipitation = new Precipitation();
            Clouds = new Clouds();
        }

        public Forecast(XmlNode node)
        {
            string fromQuery = "@from";
            string toQuery = "@to";
            string humidityQuery = "humidity/@value";
            
            From = DateTime.Parse($"{node.SelectSingleNode(fromQuery)?.InnerText:O}");
            To = DateTime.Parse($"{node.SelectSingleNode(toQuery)?.InnerText:O}");
            Wind = new Wind(node);
            Temperature = new Temperature(node);
            Pressure = new Pressure(node);
            Symbol = new Symbol(node);
            Humidity = FixDouble.ToDouble(node.SelectSingleNode(humidityQuery)?.InnerText);
            Clouds = new Clouds(node);
            Precipitation = new Precipitation(node);
        }

        public Forecast(params object[] args)
        {
            Id = int.Parse(args[0].ToString());
            From = DateTime.Parse(args[2].ToString());
            To = DateTime.Parse(args[3].ToString());
            Humidity = FixDouble.ToDouble(args[4].ToString());

            Temperature = new Temperature
            {
                UnitType = args[6].ToString(),
                Value = FixDouble.ToDouble(args[7].ToString()),
                Min = FixDouble.ToDouble(args[8].ToString()),
                Max = FixDouble.ToDouble(args[9].ToString()),
            };

            Precipitation = new Precipitation
            {
                Unit = args[11].ToString(),
                Value = FixDouble.ToDouble(args[12].ToString()),
                Type = args[13].ToString()
            };

            Pressure = new Pressure
            {
                Unit = args[15].ToString(),
                Value = FixDouble.ToDouble(args[16].ToString())
            };

            Symbol = new Symbol
            {
                Number = FixDouble.ToDouble(args[18].ToString()),
                Name = args[19].ToString(),
                Var = args[20].ToString(),
            };

            Wind = new Wind
            {
                Direction = FixDouble.ToDouble(args[22].ToString()),
                Code = args[23].ToString(),
                DirectionName = args[24].ToString(),
                Speed = FixDouble.ToDouble(args[26].ToString()),
                SpeedName = args[27].ToString(),
            };

            Clouds = new Clouds
            {
                Value = args[29].ToString(),
                All = FixDouble.ToDouble(args[30].ToString()),
            };
        }

        public string ToQuery(City city)
        {
            return $"({city.Id}, '{From}', '{To}', {FixDouble.ToString(Humidity)})";
        }

        public bool IsForecastValid()
        {
            if (DateTime.Now > To)
                return false;

            return true;
        }
    }

    public class Wind
    {
        public double Speed { get; set; }

        public string SpeedName { get; set; }

        public string DirectionName { get; set; }

        public string Code { get; set; }

        public double Direction { get; set; }

        public Wind() { }

        public Wind(XmlNode node)
        {
            string speedQuery = "windSpeed/@mps";
            string nameQuery = "windSpeed/@name";
            string dirQuery = "windDirection/@deg";
            string dirNameQuery = "windDirection/@name";
            string codeQuery = "windDirection/@code";

            Speed = FixDouble.ToDouble(node.SelectSingleNode(speedQuery)?.InnerText);
            SpeedName = node.SelectSingleNode(nameQuery)?.InnerText;

            Direction = FixDouble.ToDouble(node.SelectSingleNode(dirQuery)?.InnerText);
            DirectionName = node.SelectSingleNode(dirNameQuery)?.InnerText;

            Code = node.SelectSingleNode(codeQuery)?.InnerText;
        }

        public string ToDirQuery(Forecast f)
        {
            return $"({f.Id}, {FixDouble.ToString(Direction)}, '{Code}','{DirectionName}'),";
        }

        public string ToSpeedQuery(Forecast f)
        {
            return $"({f.Id}, {FixDouble.ToString(Speed)}, '{SpeedName}'),";
        }
    }

    public class Temperature
    {
        public string UnitType { get; set; }

        public double Value { get; set; }

        public double Min { get; set; }

        public double Max { get; set; }

        public Temperature() { }

        public Temperature(XmlNode node)
        {
            string unitQuery = "temperature/@unit";
            string valueQuery = "temperature/@value";
            string minQuery = "temperature/@min";
            string maxQuery = "temperature/@max";
            
            UnitType = node.SelectSingleNode(unitQuery)?.InnerText;
            Value = FixDouble.ToDouble(node.SelectSingleNode(valueQuery)?.InnerText);
            Min = FixDouble.ToDouble(node.SelectSingleNode(minQuery)?.InnerText);
            Max = FixDouble.ToDouble(node.SelectSingleNode(maxQuery)?.InnerText);
        }

        public string ToQuery(Forecast f)
        {
            return $"({f.Id}, '{UnitType}', {FixDouble.ToString(Value)}, " +
                $"{FixDouble.ToString(Min)}, {FixDouble.ToString(Max)}),";
        }
    }

    public class Pressure
    {
        public string Unit { get; set; }

        public double Value { get; set; }

        public Pressure() { }

        public Pressure(XmlNode node)
        {
            string unitQuery = "pressure/@unit";
            string valueQuery = "pressure/@value";

            Unit = node.SelectSingleNode(unitQuery)?.InnerText;
            Value = FixDouble.ToDouble(node.SelectSingleNode(valueQuery)?.InnerText);
        }

        public string ToQuery(Forecast f)
        {
            return $"({f.Id}, '{Unit}', {FixDouble.ToString(Value)}),";
        }
    }

    public class Precipitation
    {
        public string Unit { get; set; }

        public double Value { get; set; }

        public string Type { get; set; }

        public Precipitation() { }

        public Precipitation(XmlNode node)
        {
            string unitQuery = "precipitation/@unit";
            string valueQuery = "precipitation/@value";
            string typeQuery = "precipitation/@type";

            Unit = node.SelectSingleNode(unitQuery)?.InnerText;
            Value = FixDouble.ToDouble(node.SelectSingleNode(valueQuery)?.InnerText);
            Type = node.SelectSingleNode(typeQuery)?.InnerText;
        }

        public string ToQuery(Forecast f)
        {
            return $"({f.Id}, '{Unit}', {FixDouble.ToString(Value)}, '{Type}'),";
        }
    }

    public class Symbol
    {
        public double Number { get; set; }

        public string Name { get; set; }

        public string Var { get; set; }

        public Symbol() { }

        public Symbol(XmlNode node)
        {
            string numberQuery = "symbol/@number";
            string nameQuery = "symbol/@name";
            string varQuery = "symbol/@var";

            Name = node.SelectSingleNode(nameQuery)?.InnerText;
            Number = FixDouble.ToDouble(node.SelectSingleNode(numberQuery)?.InnerText);
            Var = node.SelectSingleNode(varQuery)?.InnerText;
        }

        public string ToQuery(Forecast f)
        {
            return $"({f.Id}, {FixDouble.ToString(Number)}, '{Name}','{Var}'),";
        }
    }

    public class Clouds
    {
        public string Value { get; set; }

        public double All { get; set; }

        public Clouds() { }

        public Clouds(XmlNode node)
        {
            string valueQuery = "clouds/@value";
            string allQuery = "clouds/@all";

            Value = node.SelectSingleNode(valueQuery)?.InnerText;
            All = FixDouble.ToDouble(node.SelectSingleNode(allQuery)?.InnerText);
        }

        public string ToQuery(Forecast f)
        {
            return $"({f.Id}, '{Value}', {FixDouble.ToString(All)}),";
        }
    }

    public static class FixDouble
    {
        public static double ToDouble(string s)
        {
            if(s == null || s == "")
                return 0;

            return double.Parse(s.ToString().Replace('.', ','));
        }

        public static string ToString(double d)
        {
            return d.ToString().Replace(',', '.');
        }
    }
}
