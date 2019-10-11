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
using System.Xml;
using Microsoft.Azure.Documents.Client;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
namespace TransformFunctions
{
    public static class TransformCCDSaveToDB
    {
        [FunctionName("TransformCCDSaveToDB")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName:"%CosmosDBNAME%",
                collectionName :"%CosmosCCDCollection%",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
            
           
            try
            {
                string coid = req.Query["id"];
                if (coid == null) coid = Guid.NewGuid().ToString();
                string spersist = req.Query["persist"];
                if (string.IsNullOrEmpty(spersist)) spersist = "true";
                bool persist = bool.Parse(spersist);
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                JObject jobj = new JObject();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(requestBody);
                string json = JsonConvert.SerializeXmlNode(doc);
                JObject ccdobj = JObject.Parse(JsonConvert.SerializeXmlNode(doc));
                string rhm = determinerhm(ccdobj);
                jobj["id"] = coid;
                jobj["rhm"] = rhm;
                jobj["location"] = req.GetIPAddress() ?? "";
                jobj["ccd"] = ccdobj;
                if (persist)
                {
                    Uri collection = UriFactory.CreateDocumentCollectionUri(Utilities.GetEnvironmentVariable("CosmosDBNAME", "hl7json"), Utilities.GetEnvironmentVariable("CosmosCCDCollection", "ccds"));
                    var inserted = await client.UpsertDocumentAsync(collection, jobj);
                    Utilities.TraceAccess(log, claimsPrincipal, client, collection, Utilities.ACTION.UPSERT, coid);
                }
                return new JsonResult(jobj);

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
            return "";
        }
       
            
          
    }
}
