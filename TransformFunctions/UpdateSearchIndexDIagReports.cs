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
            databaseName: "%CosmosDBNAME%",
            collectionName: "%CosmosHL7Collection%",
            ConnectionStringSetting = "CosmosDBConnection",
            CreateLeaseCollectionIfNotExists = true,
            LeaseCollectionPrefix = "srchupd",
            LeaseCollectionName = "leases")]IReadOnlyList<Document> input, ILogger log)
        {
            log.LogInformation("UpdateSearchIndexDiagReports function triggered");
            if (input != null && input.Count > 0)
            {
                SearchUtilities search = new SearchUtilities(log);
                log.LogInformation($"There were {input.Count} documents modified in DB..Running NLP/Seach pipeline for modified docs...");
                List<MedicalEntities> searcharr = new List<MedicalEntities>();
                foreach (Document d in input)
                {
                    string json = d.ToString();
                    StringBuilder builder = new StringBuilder();
                    var obj = JObject.Parse(json);
                    string msgtype = Utilities.getFirstField(obj["hl7message"]["MSH"]["MSH.9"]);
                    if (msgtype.ToLower().Equals("oru") || msgtype.ToLower().Equals("mdm"))
                    {
                        if (obj["hl7message"]["OBX"] != null)
                        {
                            if (obj["hl7message"]["OBX"].Type == JTokenType.Array)
                            {
                                foreach (var obx in obj["hl7message"]["OBX"])
                                {
                                    if (Utilities.getFirstField(obx["OBX.2"]).Equals("TX") || Utilities.getFirstField(obx["OBX.2"]).Equals("FT"))
                                    {
                                        builder.Append(Utilities.getFirstField(obx["OBX.5"]));
                                    }
                                }
                            }
                            else
                            {
                                var obx = obj["hl7message"]["OBX"];
                                if (Utilities.getFirstField(obx["OBX.2"]).Equals("TX") || Utilities.getFirstField(obx["OBX.2"]).Equals("FT"))
                                {
                                    builder.Append(Utilities.getFirstField(obx["OBX.5"]));
                                }
                            }
                            string report = builder.ToString();
                            report = report.UnEscapeHL7();
                            report = report.Replace(@"\\", @"\");

                            string cogurl = Utilities.GetEnvironmentVariable("CogServicesOCRURL");
                            string responseFromServer = NLPUtilities.ExtractTextUsingCogServices(Encoding.UTF8.GetBytes(report), cogurl, Utilities.GetEnvironmentVariable("CogServicesKey"));
                            if (string.IsNullOrEmpty(responseFromServer))
                            {
                                responseFromServer = NLPUtilities.ExtractTextUsingTIKA(Encoding.UTF8.GetBytes(report), Utilities.GetEnvironmentVariable("TIKAServerurl"));
                            }
                            if (responseFromServer.StartsWith("TIMEOUT~"))
                            {
                                log.LogTrace("{\"id\":\"" + Utilities.getFirstField(obj["id"]) + "\",\"status\":\"Timeout\",\"readresulturl\":\"" + responseFromServer.Split("~")[1] + "\"}");
                            }
                            if (string.IsNullOrEmpty(responseFromServer) || responseFromServer.Length<3)
                            {
                                log.LogError($"TIKA Server may have failed to parse content {(string)obj["id"]}");
                            }
                            //Send Report to NLP
                            CTakesRequest creq = new CTakesRequest()
                            {
                                Content = responseFromServer,
                                CTAKESUrl = Utilities.GetEnvironmentVariable("CTAKESServerURL"),
                                UMLSUser = Utilities.GetEnvironmentVariable("CTAKESUMLSUser"),
                                UMLSPassword = Utilities.GetEnvironmentVariable("CTAKESUMLSPassword"),
                                Format = Utilities.GetEnvironmentVariable("CTAKESFormat"),
                            };
                            var result = NLPUtilities.ExtractMedicalEntities(creq);
                            result.Id = (string)obj["id"];
                            result.Location = (string)obj["rhm"];
                            if (string.IsNullOrEmpty(result.ParsedText)) result.ParsedText = responseFromServer;
                            string doctype = "";
                            if (msgtype.Equals("MDM"))
                            {
                                doctype = Utilities.getFirstField(obj["hl7message"]["TXA"]["TXA.2"]);
                            }
                            if (doctype == null) doctype = "";
                            result.DocumentType = doctype;
                            searcharr.Add(result);
                        }



                    }

                }
                if (searcharr.Count > 0) search.UploadMedicalEntities(searcharr.ToArray());
            }
            log.LogInformation("UpdateSearchIndexDiagReports function completed");

        }
    }
}
