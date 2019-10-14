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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TransformFunctions
{
    public static class TransformHL7SaveToBlob
    {
        /* Transforms HL7 to JSON and Stores it to blob in containers by MessageType MSH-9/year/month/day/hour/specified object id or guid.
         * To Skip Transform send request parameter 'raw' in query string and it will store the HL7 message as sent
         *
         * Blob Binding has to be defined in Environment settings
         * Request should be according to the HAPI HL7OverHTTP Specification: https://hapifhir.github.io/hapi-hl7v2/hapi-hl7overhttp/specification.html
         * Responds with an MSA ACK/NAK message per the Specification
         * 
         */
        [FunctionName("TransformHL7SaveToBlob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Blob("%StorageAccountBlob%", Connection ="StorageAccount")] CloudBlobContainer container, ILogger log)
        {
            string contenttype = req.ContentType;
            log.LogInformation("C# TransformSaveToBlob HTTP trigger function fired");
            string coid = req.Query["id"];
            if (coid == null) coid = Guid.NewGuid().ToString();
            bool raw = req.Query.ContainsKey("raw");
           
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
          
            JObject jobj = null;
            try
            {
                jobj = HL7ToXmlConverter.ConvertToJObject(requestBody);
                DateTime now = DateTime.Now;
                string msgtype = (string) jobj["hl7message"]["MSH"]["MSH.9"]["MSH.9.1"];
                string ds = now.Year.ToString() + "/" + now.Month.ToString("D2") + "/" + now.Day.ToString("D2") + "/" + now.Hour.ToString("D2");
                await container.CreateIfNotExistsAsync();
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(msgtype.ToLower() + "/" + ds + "/" + coid.ToLower() + (raw ? ".hl7" : ".json"));
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes((raw ? requestBody : HL7ToXmlConverter.ConvertToJSON(jobj))), writable: false))
                {
                    await blockBlob.UploadFromStreamAsync(stream);
                }
                var retVal = new ContentResult();
                retVal.ContentType = contenttype;
                retVal.Content = Utilities.GenerateACK(jobj);
                return retVal;

            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                return new System.Web.Http.InternalServerErrorResult();
            }
        }
    }
}
