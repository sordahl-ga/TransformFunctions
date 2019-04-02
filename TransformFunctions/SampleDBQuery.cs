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
        [FunctionName("SampleDBQuery")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName:"hl7json",
                collectionName :"messages",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
                 ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
            string querystr = "select top 100 VALUE c from c where c['hl7message']['MSH']['MSH.9']['MSH.9.1']='PPR'";
            var collection = UriFactory.CreateDocumentCollectionUri("hl7json", "messages");
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
