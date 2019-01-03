using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Documents.Client;

namespace TransformFunctions
{
    public static class TransformHL7SaveToDB
    {
        [FunctionName("TransformHL7SaveToDB")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName:"hl7json",
                collectionName :"messages",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation("C# TransformSaveToDB HTTP trigger function fired");
            string coid = req.Query["id"];
            if (coid == null) coid = Guid.NewGuid().ToString();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
          
            JObject jobj = null;
            try
            {
                jobj = HL7ToXmlConverter.ConvertToJObject(requestBody);
                string rhm = determinerhm(jobj);
                jobj["id"] = coid;
                jobj["rhm"] = rhm;
                var inserted = await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri("hl7json","messages"), jobj);
                return new OkResult();

            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                return new System.Web.Http.InternalServerErrorResult();
            }
        }
        private static string determinerhm(JObject obj)
        {
            string rhm = "932";
            string msgtype = getFirstField(obj["hl7message"]["MSH"]["MSH.9"]);
            msgtype = msgtype.ToLower();
            if (msgtype.Equals("adt"))
            {
                string instance = getFirstField(obj["hl7message"]["MSH"]["MSH.3"]);
                string source = getFirstField(obj["hl7message"]["MSH"]["MSH.4"]);
                if (instance.Equals("HQ") && source.Equals("C")) rhm = "204";
                else if (instance.Equals("HQ") && source.Equals("U")) rhm = "205";
            }
            else if (msgtype.Equals("oru"))
            {
                string pv139 = getFirstField(obj["hl7message"]["PV1"]["PV1.39"]);
                var rhm205 = "COE, COM, COC, CON, COS, COU";
                var rhm204 = "J, CH, M";
                if (rhm205.IndexOf(pv139) > -1) rhm = "205";
                if (rhm204.IndexOf(pv139) > -1) rhm = "204";
            }
            else if (msgtype.Equals("orm"))
            {
                string instance = getFirstField(obj["hl7message"]["MSH"]["MSH.3"]);
                string source = getFirstField(obj["hl7message"]["MSH"]["MSH.4"]);
                if (instance.Equals("HNAM") && source.Equals("AA")) rhm = "204";
                else if (instance.Equals("HNAM") && source.Equals("CO")) rhm = "205";
            }
            return rhm;
            
        }
        private static string getFirstField(JToken o)
        {
            if (o == null) return "";
            if (o.Type == JTokenType.String) return (string)o;
            if (o.Type == JTokenType.Object) return (string)o.First;
            return "";
        }
    }
}
