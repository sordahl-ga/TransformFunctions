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
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace TransformFunctions
{
    public static class SampleDBQuery
    {
        [Disable]
        [FunctionName("SampleDBQuery")]
        /* Demonstrates how to query HL7 Json Collection in CosmosDB audits access to data with claims principal */
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName:"%CosmosDBNAME%",
                collectionName :"%CosmosHL7Collection%",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
                 ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
            string querystr = "select top 100 VALUE c from c where c['hl7message']['MSH']['MSH.9']['MSH.9.1']='PPR'";
            var collection = UriFactory.CreateDocumentCollectionUri("%CosmosDBNAME%", "%CosmosHL7Collection%");
            int pagesize = 10;
            var options = new FeedOptions() { MaxItemCount = pagesize, EnableCrossPartitionQuery = true };
            var continuationToken = string.Empty;
            var allResults = new List<Document>();
            do
            {
                if (!string.IsNullOrEmpty(continuationToken))
                {
                    options.RequestContinuation = continuationToken;
                }
                var query = await client.CreateDocumentQuery<Document>(collection, querystr, options).ToPagedResults();
                continuationToken = query.ContinuationToken;
                allResults.AddRange(query.Results);
                if (query.Results.Count > 0)
                {
                    if (options.RequestContinuation != null)
                    {
                        Utilities.TraceAccess(log, claimsPrincipal, client, collection, Utilities.ACTION.QUERY, options.RequestContinuation.ToString());
                    }
                    else
                    {
                        Utilities.TraceAccess(log, claimsPrincipal, client, collection, Utilities.ACTION.QUERY,query.Results);
                    }
   
                }
            } while (!string.IsNullOrEmpty(continuationToken));
            return new JsonResult(allResults);
        }
    }
}
