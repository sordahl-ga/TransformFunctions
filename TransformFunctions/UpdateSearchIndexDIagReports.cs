using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Linq;
using System;
using System.Text;

namespace TransformFunctions
{
    public static class UpdateSearchIndexDiagReports
    {
        [FunctionName("UpdateSearchIndexDiagReports")]
        public static void Run([CosmosDBTrigger(
            databaseName: "hl7json",
            collectionName: "messages",
            ConnectionStringSetting = "CosmosDBConnection",
            CreateLeaseCollectionIfNotExists = true,
            LeaseCollectionPrefix = "srchupd",
            LeaseCollectionName = "leases")]IReadOnlyList<Document> input, ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                SearchUtilities search = new SearchUtilities(log);
                log.LogInformation("Documents modified " + input.Count);
                List<MedicalEntities> searcharr = new List<MedicalEntities>();
                foreach(Document d in input)
                {
                    string json = d.ToString();
                    StringBuilder builder = new StringBuilder();
                    var obj = JObject.Parse(json);
                    string msgtype = Utilities.getFirstField(obj["hl7message"]["MSH"]["MSH.9"]);
                    if (msgtype.ToLower().Equals("oru"))
                    {
                        foreach (var obx in obj["hl7message"]["OBX"])
                        {
                            if (Utilities.getFirstField(obx["OBX.2"]).Equals("TX"))
                            {
                                builder.Append(Utilities.getFirstField(obx["OBX.5"]));
                            }
                        }
		
                    }
                    string report = builder.ToString();
                    //Send Report to NLP
                    CTakesRequest creq = new CTakesRequest()
                    {
                        Content = report,
                        CTAKESUrl = Utilities.GetEnvironmentVariable("CTAKESServerURL"),
                        UMLSUser = Utilities.GetEnvironmentVariable("CTAKESUMLSUser"),
                        UMLSPassword = Utilities.GetEnvironmentVariable("CTAKESUMLSPassword"),
                        Format = Utilities.GetEnvironmentVariable("CTAKESFormat"),
                    };
                    var result = NLPUtilities.ExtractMedicalEntities(creq);
                    result.Id = (string)obj["id"];
                    result.Location = (string)obj["rhm"];
                    searcharr.Add(result);
                    
                }
                search.UploadMedicalEntities(searcharr.ToArray());
            }
        }
       
       
    }
}
