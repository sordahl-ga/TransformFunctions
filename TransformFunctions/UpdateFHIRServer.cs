using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TransformFunctions
{
    public static class UpdateFHIRServer
    {
        [Disable]
        [FunctionName("UpdateFHIRServer")]
        public static void Run([CosmosDBTrigger(
            databaseName: "%CosmosDBNAME%",
            collectionName: "%CosmosHL7Collection%",
            ConnectionStringSetting = "CosmosDBConnection",
            CreateLeaseCollectionIfNotExists = true,
            LeaseCollectionPrefix = "fhirupd",
            LeaseCollectionName = "leases")]IReadOnlyList<Document> input, ILogger log)
        {
           
            if (string.IsNullOrEmpty(Utilities.GetEnvironmentVariable("FHIRServer")) || !bool.Parse(Utilities.GetEnvironmentVariable("FHIRTransformEnabled","false"))) return;
            if (input != null && input.Count > 0)
            {
                log.LogInformation("UpdateFHIRServer Documents modified " + input.Count);
                foreach (Document d in input)
                {
                    string json = d.ToString();
                    var obj = JObject.Parse(json);
                    string msgtype = Utilities.getFirstField(obj["hl7message"]["MSH"]["MSH.9"]);
                    if (msgtype.ToLower().Equals("orm"))
                    {
                        var s = TransformToFHIR(obj, "api/ORM2FHIR");
                        log.LogTrace($"The result is {s}");
                        UpdateFHIR(s);
                    } else if (msgtype.ToLower().Equals("adt"))
                    {
                        var s = TransformToFHIR(obj, "api/ADT2FHIR");
                        log.LogTrace($"The result is {s}");
                        UpdateFHIR(s);
                    } else if (msgtype.ToLower().Equals("oru"))
                    {
                        var s = TransformToFHIR(obj, "api/ORU2FHIR");
                        log.LogTrace($"The result is {s}");
                        UpdateFHIR(s);
                    }


                }
                
            }
        }
        private static void UpdateFHIR(string json)
        {
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
            // Create a request using a URL that can receive a post. 
            WebRequest request = WebRequest.Create(Utilities.GetEnvironmentVariable("FHIRServer") + "Bundle");
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Create POST data and convert it to a byte array.
            request.Headers.Add("Accept:application/json");
            request.Headers.Add("Content-Type:application/json");
            // Set the ContentType property of the WebRequest.
            //request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            using (Stream dataStream = request.GetRequestStream())
            {
                // Write the data to the request stream.
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            //Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            // Get the stream containing content returned by the server.
            string responseFromServer = "";
            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                using (StreamReader reader = new StreamReader(dataStream))
                {
                    // Read the content.
                    responseFromServer = reader.ReadToEnd();
                }
            }
            
        
        }
        private static string TransformToFHIR(JObject obj,string api)
        {
            string msgtype = Utilities.getFirstField(obj["hl7message"]["MSH"]["MSH.9"]);
            string data = JsonConvert.SerializeObject(obj["hl7message"]);
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(data);
            // Create a request using a URL that can receive a post. 
            WebRequest request = WebRequest.Create(Utilities.GetEnvironmentVariable("FHIRTransformURL") + "/" + api + "?id=" + (string)obj["id"]);
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Create POST data and convert it to a byte array.
            request.Headers.Add("Accept:application/json");
            request.Headers.Add("Content-Type:application/json");
            request.Headers.Add("x-functions-key:" + Utilities.GetEnvironmentVariable("FHIRTransformKey"));
            // Set the ContentType property of the WebRequest.
            //request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            using (Stream dataStream = request.GetRequestStream())
            {
                // Write the data to the request stream.
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            //Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            // Get the stream containing content returned by the server.
            string responseFromServer = "";
            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                using (StreamReader reader = new StreamReader(dataStream))
                {
                    // Read the content.
                    responseFromServer = reader.ReadToEnd();
                }
            }
            return responseFromServer;
        }
    }
}
