/* 
* 2018 Microsoft Corp
* 
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS”
* AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
* THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
* ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
* FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
* HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
* OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
* OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
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
        public static void Run([BlobTrigger("%StorageAccountBlob%/ingest/documents/{name}", Connection = "StorageAccount")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation("NLP Extract Entities File triggered by ingest/documents/" + name);
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
                log.LogInformation("Calling CogServices/TIKA to Extract Text from hl7json/ingest/documents/" + name);
                string cogurl = Utilities.GetEnvironmentVariable("CogServicesOCRURL");
                log.LogInformation("Trying CogServices...");
                string responseFromServer = NLPUtilities.ExtractTextUsingCogServices(byteArray, cogurl, Utilities.GetEnvironmentVariable("CogServicesKey"));
                if (string.IsNullOrEmpty(responseFromServer))
                {
                    log.LogInformation("No extract Trying TIKA...");
                    responseFromServer = NLPUtilities.ExtractTextUsingTIKA(byteArray, Utilities.GetEnvironmentVariable("TIKAServerurl"));
                }
                if (responseFromServer.StartsWith("TIMEOUT~"))
                {
                    log.LogTrace("CogServiceExtract Timeout: {\"id\":\"" + coid + "\",\"status\":\"Timeout\",\"readresulturl\":\"" + responseFromServer.Split("~")[1] + "\"}");
                }
              
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
                    result.DocumentType = name;
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
