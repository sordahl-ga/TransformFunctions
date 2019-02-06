using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
namespace TransformFunctions
{
    public static class StringExtentions
    {
      
            public static string UnEscapeHL7(this string str)
            {
                return str.Replace("\\T\\", "&").Replace("\\S\\", "^").Replace("\\E\\", "\\").Replace("\\R\\", "~").Replace("\\.br\\","\\n");
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
