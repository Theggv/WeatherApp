using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherApp
{
    public class Country : IEquatable<Country>
    {
        public string Name { get; set; }
        
        public string Code { get; set; }


        public bool Equals(Country other)
        {
            if (other == null)
                return false;

            return Code == other.Code;
        }

        public override bool Equals(object obj) => Equals(obj as Country);


        public override int GetHashCode() => (Name, Code).GetHashCode();
    }
}
