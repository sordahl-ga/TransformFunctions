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
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text;

namespace TransformFunctions
{
   public static class NLPExtractEntitiesHttp
    {
        
        [FunctionName("NLPExtractEntitiesHttp")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
                log.LogInformation("NLP Extract Entities Http called");
                try
                {
                    
                    string coid = req.Query["id"];
                    if (coid == null) coid = Guid.NewGuid().ToString();
                    string loc = req.Query["location"];
                    if (loc == null) loc = "Adhoc Document Addition";
                    string updatesearch = req.Query["updatesearch"];
                    byte[] byteArray = null;
                    // Read the post data into byte array
                    using (var stream = new MemoryStream())
                    {
                    await req.Body.CopyToAsync(stream);
                    byteArray = stream.ToArray();
                    }
                    //string responseFromServer = Encoding.UTF8.GetString(byteArray);
                    string responseFromServer = NLPUtilities.ExtractTextUsingTIKA(byteArray, Utilities.GetEnvironmentVariable("TIKAServerURL"));
                    //Extract Reports From Content (Auto-Detect Medical Exchange Formats (CDA, HL7, FHIR))
                    List<string> medreports = NLPUtilities.ExtractMedicalReportData(responseFromServer,log);
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

                        var result = NLPUtilities.ExtractMedicalEntities(creq);
                        result.Id = coid;
                        result.Location = loc;
                        retVal.Add(result);
                    }
                    if (updatesearch !=null)
                    {
                        SearchUtilities su = new SearchUtilities(log);
                        su.UploadMedicalEntities(retVal.ToArray());
                    }

                    return new JsonResult(retVal);
                }
                catch (System.Exception e)
                {
                    log.LogError(e, e.Message);
                    return new System.Web.Http.InternalServerErrorResult();
                }
           
        }
        


    }
}
