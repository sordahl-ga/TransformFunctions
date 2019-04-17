using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Xml;
using Microsoft.Azure.Documents.Client;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
namespace TransformFunctions
{
    public static class TransformCCDSaveToDB
    {
        [FunctionName("TransformCCDSaveToDB")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName:"%CosmosDBNAME%",
                collectionName :"%CosmosCCDCollection%",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
            
           
            try
            {
                string coid = req.Query["id"];
                if (coid == null) coid = Guid.NewGuid().ToString();
                string spersist = req.Query["persist"];
                if (string.IsNullOrEmpty(spersist)) spersist = "true";
                bool persist = bool.Parse(spersist);
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                JObject jobj = new JObject();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(requestBody);
                string json = JsonConvert.SerializeXmlNode(doc);
                JObject ccdobj = JObject.Parse(JsonConvert.SerializeXmlNode(doc));
                string rhm = determinerhm(ccdobj);
                jobj["id"] = coid;
                jobj["rhm"] = rhm;
                jobj["location"] = req.GetIPAddress() ?? "";
                jobj["ccd"] = ccdobj;
                if (persist)
                {
                    Uri collection = UriFactory.CreateDocumentCollectionUri("hl7json", "ccds");
                    var inserted = await client.UpsertDocumentAsync(collection, jobj);
                    Utilities.TraceAccess(log, claimsPrincipal, client, collection, Utilities.ACTION.UPSERT, coid);
                }
                return new JsonResult(jobj);

            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                var retVal = new ContentResult();
                retVal.ContentType = "text/plain";
                retVal.Content = e.Message;
                retVal.StatusCode = 500;
                return retVal;
            }
        }
        private static string determinerhm(JObject obj)
        {
            return "";
        }
       
            
          
    }
}
