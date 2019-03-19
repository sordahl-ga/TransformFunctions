using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Xml;
using System;
namespace TransformFunctions
{
    public static class CCDtoJSON
    {
        [FunctionName("CCDtoJSON")]
        public static void Run([BlobTrigger("hl7json/ingest/ccdbatch/{name}", Connection = "StorageAccount")]Stream myBlob, string name, ILogger log)
        {
            try
            {
                string x = null;
                using (StreamReader reader = new StreamReader(myBlob))
                {
                    x = reader.ReadToEndAsync().GetAwaiter().GetResult();
                }
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(x);
                string json = JsonConvert.SerializeXmlNode(doc);
                log.LogInformation(json);
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
            }
        }
    }
}
