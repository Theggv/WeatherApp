using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherApp
{
    public static class JSON
    {
        public static JSONObject Parse(string json)
        {
            json = json.Replace("\r", "").Replace("\n", "").Trim();

            if (json[0] == '{')
                return new JSONObject(json);
            else
                return new JSONArray(json);
        }
    }

    public class JSONObject
    {
        private string _json;
        private List<JSONProperty> _values;

        public JSONObject this[string index]
        {
            get
            {
                foreach(var obj in _values)
                {
                    if (obj.Key == index)
                        return obj.Value;
                }

                return null;
            }
        }
        public virtual JSONObject this[int index]
        {
            get
            {
                if (index < 0 || index >= _values.Count)
                    return null;

                return _values[index];
            }
        }

        public JSONObject()
        {
            _values = new List<JSONProperty>();
        }

        public JSONObject(string json)
        {
            _json = json.Replace("\r", "").Replace("\n", "").Trim();

            _values = new List<JSONProperty>();

            var quotes = new List<Sign>();

            int depth = 0;

            bool isOpenQuote = false;

            for (int i = 0; i < _json.Length; ++i)
            {
                if (_json[i] == '{')
                {
                    if (depth++ <= 2)
                        quotes.Add(new Sign { Char = '{', Pos = i, Depth = depth });
                }
                else if (_json[i] == '}')
                {
                    if (--depth <= 2)
                        quotes.Add(new Sign { Char = '}', Pos = i, Depth = depth });
                }
                else if (_json[i] == '[')
                {
                    if (depth++ <= 2)
                        quotes.Add(new Sign { Char = '[', Pos = i, Depth = depth });
                }
                else if (_json[i] == ']')
                {
                    if (--depth <= 2)
                        quotes.Add(new Sign { Char = ']', Pos = i, Depth = depth });
                }
                else if (_json[i] == '\"')
                {
                    depth = !isOpenQuote ? depth + 1 : depth - 1;

                    isOpenQuote = !isOpenQuote;
                }
                else if (depth == 1)
                {
                    if (_json[i] == ':')
                        quotes.Add(new Sign { Char = ':', Pos = i, Depth = depth });
                    else if (_json[i] == ',')
                        quotes.Add(new Sign { Char = ',', Pos = i, Depth = depth });
                }
            }

            if (quotes.Count == 0)
                return;

            depth = 1;

            FindProperties(quotes, depth);
        }

        private void FindProperties(List<Sign> quotes, int depth)
        {
            if (quotes.Where(s => s.Char == ':' && s.Depth == depth).Count() == 0)
                return;

            for (int i = 0; i < quotes.Count; i++)
            {
                if (quotes[i].Char == ':' && quotes[i].Depth == depth)
                {
                    // property found
                    
                    int startPos = quotes[i - 1].Pos + 1;
                    int endPos;

                    if (quotes[i + 1].Char != ',' &&
                        (quotes[i + 1].Char == '{' || quotes[i + 1].Char == '['))
                        endPos = FindCloseBracket(quotes[i + 1].Pos) + 1;
                    else
                        endPos = quotes[i + 1].Pos;

                    _values.Add(new JSONProperty(_json.Substring(startPos, endPos - startPos)));
                }
            }
        }

        private int FindCloseBracket(int index)
        {
            int depth = 0;

            for (int i = index + 1; i < _json.Length; ++i)
            {
                if (_json[i] == '{' || _json[i] == '[')
                    depth++;
                else if (_json[i] == '}' || _json[i] == ']')
                {
                    depth--;

                    if (depth == -1)
                        return i;
                }
            }

            return -1;
        }

        sealed protected class Sign
        {
            public char Char { get; set; }

            public int Pos { get; set; }

            public int Depth { get; set; }
        }
    }

    public class JSONArray: JSONObject
    {
        private string _json;
        private List<JSONObject> _values;

        public override JSONObject this[int index]
        {
            get
            {
                if (index < 0 || index >= _values.Count)
                    return null;

                return _values[index];
            }
        }

        public JSONArray()
        {
            _values = new List<JSONObject>();
        }

        public JSONArray(string json)
        {
            _json = json.Replace("\r", "").Replace("\n", "").Trim();

            _values = new List<JSONObject>();

            var quotes = new List<Sign>();

            bool isOpenQuote = false;

            int depth = 0;

            for (int i = 0; i < _json.Length; ++i)
            {
                if (_json[i] == '{')
                {
                    if (depth++ <= 2)
                        quotes.Add(new Sign { Char = '{', Pos = i, Depth = depth });
                }
                else if (_json[i] == '}')
                {
                    if (--depth <= 2)
                        quotes.Add(new Sign { Char = '}', Pos = i, Depth = depth });
                }
                else if (_json[i] == '[')
                {
                    if (depth++ <= 2)
                        quotes.Add(new Sign { Char = '[', Pos = i, Depth = depth });
                }
                else if (_json[i] == ']')
                {
                    if (--depth <= 2)
                        quotes.Add(new Sign { Char = ']', Pos = i, Depth = depth });
                }
                else if (_json[i] == '\"')
                {
                    depth = !isOpenQuote ? depth + 1 : depth - 1;

                    isOpenQuote = !isOpenQuote;
                }
                else if (depth == 1)
                {
                    if (_json[i] == ':')
                        quotes.Add(new Sign { Char = ':', Pos = i, Depth = depth });
                    else if (_json[i] == ',')
                        quotes.Add(new Sign { Char = ',', Pos = i, Depth = depth });
                }
            }

            if (quotes.Count == 0)
                return;

            depth = 1;
            
            FindObjects(quotes, depth);
        }

        private void FindObjects(List<Sign> quotes, int depth)
        {
            for (int i = 1; i < quotes.Count; i++)
            {
                if (quotes[i].Char == '{' && quotes[i].Depth == depth + 1)
                {
                    // object found

                    int startPos = quotes[i].Pos;
                    int endPos = FindCloseBracket(startPos);

                    _values.Add(new JSONObject(_json.Substring(startPos, endPos - startPos + 1)));
                }
                else if (quotes[i].Char == '[' && quotes[i].Depth == depth + 1)
                {
                    // array found

                    int startPos = quotes[i].Pos;
                    int endPos = FindCloseBracket(startPos);

                    _values.Add(new JSONArray(_json.Substring(startPos, endPos - startPos + 1)));
                }
                else if (quotes[i].Char == ',' && quotes[i].Depth == depth &&
                    (quotes[i - 1].Char == ',' || quotes[i - 1].Char == '['))
                {
                    // element found

                    int startPos = quotes[i - 1].Pos + 1;
                    int endPos = quotes[i].Pos - 1;

                    _values.Add(new JSONElement(_json.Substring(startPos, endPos - startPos + 1)));
                }
                else if (i == quotes.Count - 1 && 
                    (quotes[i - 1].Char == ',' || quotes[i - 1].Char == '['))
                {
                    // array with 1 element

                    int startPos = quotes[i - 1].Pos + 1;
                    int endPos = quotes[i].Pos - 1;

                    _values.Add(new JSONElement(_json.Substring(startPos, endPos - startPos + 1)));
                }
            }
        }

        private int FindCloseBracket(int index)
        {
            int depth = 0;

            for (int i = index + 1; i < _json.Length; ++i)
            {
                if (_json[i] == '{' || _json[i] == '[')
                    depth++;
                else if (_json[i] == '}' || _json[i] == ']')
                {
                    depth--;

                    if (depth == -1)
                        return i;
                }
            }

            return -1;
        }
    }

    public class JSONProperty : JSONObject
    {
        public string Key { get; set; }

        public JSONObject Value { get; set; }

        public JSONProperty(string json)
        {
            for (int i = 0; i < json.Length; ++i)
            {
                if (json[i] == ':')
                {
                    Key = json.Substring(0, i).Replace("\"", "").Trim();

                    var value = json.Substring(i + 1).Trim();

                    if (value[0] == '{')
                        Value = new JSONObject(value);
                    else if (value[0] == '[')
                        Value = new JSONArray(value);
                    else
                        Value = new JSONElement(value);

                    break;
                }
            }
        }
    }

    public class JSONElement: JSONObject
    {
        public object Value { get; set; }

        public JSONElement(string json)
        {
            Value = json.Replace("\"", "").Trim();
        }
    }
}
