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

namespace TransformFunctions
{
    public static class TransformHL7SaveToBlob
    {
        [FunctionName("TransformHL7SaveToBlob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Blob("hl7json", Connection ="StorageAccount")] CloudBlobContainer container, ILogger log)
        {
            log.LogInformation("C# TransformSaveToBlob HTTP trigger function fired");
            string coid = req.Query["id"];
            if (coid == null) coid = Guid.NewGuid().ToString();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string json = "";
            try
            {
                json = HL7ToXmlConverter.ConvertToJSON(requestBody);
                DateTime now = DateTime.Now;
                string ds = now.Year.ToString() + "/" + now.Month.ToString() + "/" + now.Day.ToString() + "/" + now.Hour.ToString();
                await container.CreateIfNotExistsAsync();
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(ds + "/" + coid.ToLower() + ".json");
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json), writable: false))
                {
                    await blockBlob.UploadFromStreamAsync(stream);
                }
                return new OkResult();

            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                return new System.Web.Http.InternalServerErrorResult();
            }
        }
    }
}
