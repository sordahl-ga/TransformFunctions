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
namespace TransformFunctions
{
    public static class HL7toJSON
    {
        [FunctionName("HL7toJSON")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HL7toJSON HTTP trigger function fired");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string json = "";
            try
            {
                json = JsonConvert.SerializeXmlNode(HL7ToXmlConverter.ConvertToXml(requestBody));
                JObject o = JObject.Parse(json);
                return new JsonResult(o["hl7message"]);


            } catch (Exception e)
            {
                log.LogError(e, e.Message);
                return new BadRequestObjectResult("Error: " + e.Message);
            }
            
        }
    }
}
