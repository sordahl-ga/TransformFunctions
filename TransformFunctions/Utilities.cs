using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;

namespace TransformFunctions
{
    public class Utilities
    {
        public enum ACTION
        {
            CREATE,
            UPSERT,
            QUERY,
            DELETE,
            OTHER
            
        }
        private static string extractClaims(ClaimsPrincipal p)
        {

            StringBuilder retVal = new StringBuilder();
            if (p != null)
            {
                ClaimsIdentity identity = p.Identity as ClaimsIdentity;
                foreach (var claim in identity.Claims)
                {
                    retVal.Append($"{claim.Type} = {claim.Value};");
                }
            }
            return retVal.ToString();
        }
        public static void TraceAccess(ILogger logger, ClaimsPrincipal p, DocumentClient client, Uri documentCollection, ACTION action, IEnumerable<Document> docs)
        {
            foreach (Document d in docs)
            {
                TraceAccess(logger, p, client, documentCollection, action, d.Id);
            }
        }
        public static void TraceAccess(ILogger logger, ClaimsPrincipal p, DocumentClient client, Uri documentCollection, ACTION action ,string idortoken)
        {
               logger.LogTrace($"AuditEvent Action: {Enum.GetName(typeof(ACTION),action)} Destination: {client.ServiceEndpoint.OriginalString} DB-Collection: {documentCollection.ToString()} DocId-Token: {idortoken} Claims:" + extractClaims(p)); 
        }
        public static string GenerateACK(JObject obj)
        {
            // create a HL7Message object using the original message as the source to obtain details to reflect back in the ACK message
            string trigger = getFirstField(obj["hl7message"]["MSH"]["MSH.9"]["MSH.9.2"]);
            string originatingApp = getFirstField(obj["hl7message"]["MSH"]["MSH.3"]);
            string originatingSite = getFirstField(obj["hl7message"]["MSH"]["MSH.4"]);
            string messageID = getFirstField(obj["hl7message"]["MSH"]["MSH.10"]);
            string processingID = getFirstField(obj["hl7message"]["MSH"]["MSH.11"]);
            string hl7Version = getFirstField(obj["hl7message"]["MSH"]["MSH.12"]);
            DateTime now = DateTime.Now;
            string ackTimestamp = now.Year.ToString() + now.Month.ToString() + now.Day.ToString() + now.Hour.ToString() + now.Minute.ToString();

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
