using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;

namespace TransformFunctions
{
    public static class PublishChange
    {
       
        [FunctionName("PublishChange")]
        public static void Run([CosmosDBTrigger(
            databaseName: "hl7json",
            collectionName: "messages",
            ConnectionStringSetting = "CosmosDBConnection",
            CreateLeaseCollectionIfNotExists = true,
            LeaseCollectionPrefix = "hl7pub",
            LeaseCollectionName = "leases")]IReadOnlyList<Document> input,
            [EventHub("hl7events", Connection = "EventHubConnectionString")] ICollector<EventData> outputMessages,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation($"Publishing {input.Count} hl7messages to hl7events.");
                foreach (Document d in input)
                {
                        string json = d.ToString();
                        outputMessages.Add(new EventData(Encoding.UTF8.GetBytes(json)));
                }
            }
        }

    }
}
