using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TransformFunctions
{
    public static class TransformHL7SaveToBlob
    {
        [FunctionName("TransformHL7SaveToBlob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Blob("%StorageAccountBlob%", Connection ="StorageAccount")] CloudBlobContainer container, ILogger log)
        {
            string contenttype = req.ContentType;
            log.LogInformation("C# TransformSaveToBlob HTTP trigger function fired");
            string coid = req.Query["id"];
            if (coid == null) coid = Guid.NewGuid().ToString();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
          
            JObject jobj = null;
            try
            {
                jobj = HL7ToXmlConverter.ConvertToJObject(requestBody);
                DateTime now = DateTime.Now;
                string msgtype = (string) jobj["hl7message"]["MSH"]["MSH.9"]["MSH.9.1"];
                string ds = now.Year.ToString() + "/" + now.Month.ToString("D2") + "/" + now.Day.ToString("D2") + "/" + now.Hour.ToString("D2");
                await container.CreateIfNotExistsAsync();
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(msgtype.ToLower() + "/" + ds + "/" + coid.ToLower() + ".json");
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(HL7ToXmlConverter.ConvertToJSON(jobj)), writable: false))
                {
                    await blockBlob.UploadFromStreamAsync(stream);
                }
                var retVal = new ContentResult();
                retVal.ContentType = contenttype;
                retVal.Content = Utilities.GenerateACK(jobj);
                return retVal;

            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                return new System.Web.Http.InternalServerErrorResult();
            }
        }
    }
}
