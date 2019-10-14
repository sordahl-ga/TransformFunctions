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
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Documents.Client;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Text;

namespace TransformFunctions
{
    public static class TransformHL7SaveToDB
    {
        /* Transforms HL7 to JSON and Stores it to a cosmosdb extracted mette data is: rhm = Sending Application and Sending Facility
         * Query Parameters are id:"Unique Document Id (if null one is generated), 
         *
         * databaseName Binding has to be defined in Environment settings variable CosmosDBNAME
         * collectionName Binding has to be defined in Environment settings variable CosmosHL7Collection
         * 
         * When using AuthorizationLevel.User will log traceaccess about the prinicipal in the token for access in the db
         * 
         * Request should be according to the HAPI HL7OverHTTP Specification: https://hapifhir.github.io/hapi-hl7v2/hapi-hl7overhttp/specification.html
         * Responds with an MSA ACK/NAK message per the Specification
         * 
        */
        [FunctionName("TransformHL7SaveToDB")]
        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName:"%CosmosDBNAME%",
                collectionName :"%CosmosHL7Collection%",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
          
            string contenttype = string.IsNullOrEmpty(req.ContentType) ? "application/hl7-v2+er7; charset=utf-8" : req.ContentType;
            log.LogInformation("C# TransformSaveToDB HTTP trigger function fired");
            string coid = req.Query["id"];
            if (coid == null) coid = Guid.NewGuid().ToString();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
          
            JObject jobj = null;
            try
            {
                jobj = HL7ToXmlConverter.ConvertToJObject(requestBody);
                string rhm = determinerhm(jobj);
                jobj["id"] = coid;
                jobj["rhm"] = rhm;
                jobj["location"] = req.GetIPAddress() ?? "";
                Uri collection = UriFactory.CreateDocumentCollectionUri(Utilities.GetEnvironmentVariable("CosmosDBNAME", "hl7json"),Utilities.GetEnvironmentVariable("CosmosHL7Collection", "messages"));
                var inserted = await client.UpsertDocumentAsync(collection, jobj);
                Utilities.TraceAccess(log, claimsPrincipal, client, collection, Utilities.ACTION.UPSERT, coid);
                var retVal = new ContentResult();
                retVal.ContentType = contenttype;
                retVal.Content = Utilities.GenerateACK(jobj);
                return retVal;
                

            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                var retVal = new ContentResult();
                retVal.ContentType = "text/plain";
                retVal.Content = e.Message;
                retVal.StatusCode = 500;
                return retVal;
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
