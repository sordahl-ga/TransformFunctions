using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
namespace TransformFunctions
{
    public class Utilities
    {
        public static string GenerateACK(JObject obj)
        {
            // create a HL7Message object using the original message as the source to obtain details to reflect back in the ACK message
            string trigger = getFirstField(obj["hl7message"]["MSH"]["MSH.9"]["MSH.9.2"]);
            string originatingApp = getFirstField(obj["hl7message"]["MSH"]["MSH.3"]);
            string originatingSite = getFirstField(obj["hl7message"]["MSH"]["MSH.4"]);
            string messageID = getFirstField(obj["hl7message"]["MSH"]["MSH.10"]);
            string processingID = getFirstField(obj["hl7message"]["MSH"]["MSH.11"]);
            string hl7Version = getFirstField(obj["hl7message"]["MSH"]["MSH.12"]);
            string ackTimestamp = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString();

            StringBuilder ACKString = new StringBuilder();
            ACKString.Append("MSH|^~\\&|AzureHL7Listener|AzureHL7Listener|" + originatingSite + "|" + originatingApp + "|" + ackTimestamp + "||ACK^" + trigger + "|" + messageID + "|" + processingID + "|" + hl7Version);
            ACKString.Append((char)0x0D);
            ACKString.Append("MSA|AA|" + messageID);
            ACKString.Append((char)0x0D);
            return ACKString.ToString();
        }
        public static string getFirstField(JToken o)
        {
            if (o == null) return "";
            if (o.Type == JTokenType.String) return (string)o;
            if (o.Type == JTokenType.Object) return (string)o.First;
            
            return "";
        }
        public static string GetEnvironmentVariable(string name,string defval=null)
        {
            var v = System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            return (v == null && defval != null ? defval : v);
        }
    }
}
