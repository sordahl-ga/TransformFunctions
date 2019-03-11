using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace TransformFunctions
{
    public static class StringExtentions
    {
        private static Regex REGEX = new Regex(@"\\X[0-9A-F]{2,10}\\");
        private static string parseHex(string hex)
        {

            string s = hex.Substring(2, hex.Length - 3);
            List<char> convert = new List<char>();
            while (s.Length > 1)
            {
                string p = s.Substring(0, 2);
                var c = (char)Int16.Parse(p, NumberStyles.AllowHexSpecifier);
                convert.Add(c);
                s = s.Substring(2);
            }
            return new string(convert.ToArray());

        }
        private static string UnEscapeHL7Hex(string s)
        {
            while (true)
            {
                Match match = REGEX.Match(s);
                if (match.Success)
                {
                    var r = parseHex(match.Value);
                    s = s.Replace(match.Value, r);

                }
                else
                {
                    break;
                }
            }
            return s;
        }
        public static string UnEscapeHL7(this string str)
        {
                var r =  str.Replace("\\T\\", "&").Replace("\\S\\", "^").Replace("\\E\\", "\\").Replace("\\R\\", "~").Replace("\\.br\\","\\n");
                return UnEscapeHL7Hex(r);
        }
            public static string GetFirstField(this JToken o)
            {
                if (o == null) return "";
                if (o.Type == JTokenType.String) return (string)o;
                if (o.Type == JTokenType.Object) return (string)o.First;
                return "";
            }

    }
}
