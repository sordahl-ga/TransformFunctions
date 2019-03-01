using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using System.Text;
using System;
using Newtonsoft.Json.Linq;

namespace TransformFunctions
{
    public static class HL7BatchIngest
    {
        [FunctionName("HL7BatchIngest")]
        public static void Run([BlobTrigger("hl7json/ingest/hl7batch/{name}", Connection = "StorageAccount")]Stream myBlob,
            [CosmosDB(
                databaseName:"hl7json",
                collectionName :"messages",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client, string name, ILogger log)
        {
            log.LogInformation($"Processing HL7 Batch File blob hl7json/ingest/hl7batch/{name}: \n Size: {myBlob.Length} Bytes");
            string line;
            int total = 0;
            StringBuilder message = new StringBuilder();
            using (StreamReader reader = new StreamReader(myBlob))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("MSH") && message.Length > 0)
                    {
                        processMessage(message.ToString(), client, log);
                        total++;
                        message.Clear();
                    }
                    message.Append(line);
                    message.Append("\r");
                        
                }
                if (message.Length > 0)
                {
                    processMessage(message.ToString(), client, log);
                    total++;
                }
            }
            log.LogInformation($"Successfully processed {total} messages from HL7 Batch blob hl7json/ingest/hl7batch/{name}");
                
        }
        private static async void processMessage(string message, DocumentClient client,ILogger logger)
        {
            string coid = Guid.NewGuid().ToString();
            JObject jobj = null;
            var metadata = HL7MetaDataLoader.Instance.GetMetaDataFromMessage(message);
            try
            {
                jobj = HL7ToXmlConverter.ConvertToJObject(message,metadata);
                string rhm = determinerhm(jobj);
                jobj["id"] = coid;
                jobj["rhm"] = rhm;
                var inserted = await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri("hl7json", "messages"), jobj);
                logger.LogInformation($"Message id {coid} from {rhm} added to Database");
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                
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
