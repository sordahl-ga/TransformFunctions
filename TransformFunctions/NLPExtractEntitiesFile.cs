using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace TransformFunctions
{
    public static class NLPExtractEntitiesFile
    {
        [FunctionName("NLPExtractEntitiesFile")]
        public static void Run([BlobTrigger("hl7json/ingest/documents/{name}", Connection = "StorageAccount")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation("NLP Extract Entities File triggered by hl7json/ingest/documents/" + name);
            try
            {

                string coid = name;
                int dirend = coid.LastIndexOf("/");
                if (dirend > -1) coid = coid.Substring(dirend + 1);
                int extbegin = coid.LastIndexOf(".");
                if (extbegin > -1) coid = coid.Substring(0, extbegin);
                string loc = "ingest/documents/" + name;
                byte[] byteArray = null;
                using (MemoryStream ms = new MemoryStream())
                {
                    myBlob.CopyTo(ms);
                    byteArray = ms.ToArray();
                }
                log.LogInformation("Calling TIKA to Extract Text from hl7json/ingest/documents/" + name);
                string responseFromServer = NLPUtilities.ExtractTextUsingTIKA(byteArray, Utilities.GetEnvironmentVariable("TIKAServerURL"));
                //string responseFromServer = System.Text.Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                log.LogInformation("Extracting Medical Reports from hl7json/ingest/documents/" + name);
                //Extract Reports From Content (Auto-Detect Medical Exchange Formats (CDA, HL7, FHIR))
                List<string> medreports = NLPUtilities.ExtractMedicalReportData(responseFromServer, log);
                List<MedicalEntities> retVal = new List<MedicalEntities>();
                foreach (string medreport in medreports)
                {
                    CTakesRequest creq = new CTakesRequest()
                    {
                        Content = medreport,
                        CTAKESUrl = Utilities.GetEnvironmentVariable("CTAKESServerURL"),
                        UMLSUser = Utilities.GetEnvironmentVariable("CTAKESUMLSUser"),
                        UMLSPassword = Utilities.GetEnvironmentVariable("CTAKESUMLSPassword"),
                        Format = Utilities.GetEnvironmentVariable("CTAKESFormat"),
                    };
                    log.LogInformation("Calling CTAKES to extract medical entities from hl7json/ingest/documents/" + name);
                    var result = NLPUtilities.ExtractMedicalEntities(creq);
                    result.Id = coid;
                    result.Location = loc;
                    retVal.Add(result);
                }
                log.LogInformation("Updateing search index with content and medical entities from hl7json/ingest/documents/" + name);
                SearchUtilities su = new SearchUtilities(log);
                su.UploadMedicalEntities(retVal.ToArray());
                log.LogInformation("Succesfully Completed processing of hl7json/ingest/documents/" + name);

            }
            catch (System.Exception e)
            {
                log.LogError(e, e.Message);
              
            }
        }
    }
}
