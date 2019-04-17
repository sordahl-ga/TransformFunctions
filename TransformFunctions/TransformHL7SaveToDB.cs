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
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Text;

namespace TransformFunctions
{
    public static class TransformHL7SaveToDB
    {
        [FunctionName("TransformHL7SaveToDB")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName:"%CosmosDBNAME%",
                collectionName :"%CosmosHL7Collection%",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
            string contenttype = string.IsNullOrEmpty(req.ContentType) ? "application/hl7-v2+er7; charset=utf-8" : req.ContentType;
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
                jobj["location"] = req.GetIPAddress() ?? "";
                Uri collection = UriFactory.CreateDocumentCollectionUri("hl7json", "messages");
                var inserted = await client.UpsertDocumentAsync(collection, jobj);
                Utilities.TraceAccess(log, claimsPrincipal, client, collection, Utilities.ACTION.UPSERT, coid);
                var retVal = new ContentResult();
                retVal.ContentType = contenttype;
                retVal.Content = Utilities.GenerateACK(jobj);
                return retVal;
                

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
            string instance = Utilities.getFirstField(obj["hl7message"]["MSH"]["MSH.3"]);
            string source = Utilities.getFirstField(obj["hl7message"]["MSH"]["MSH.4"]);
            return (instance != null ? instance + (source ?? "") : "");
        }
        
    }
}
