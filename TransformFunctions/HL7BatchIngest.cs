using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using System.Text;
using System;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.IO.Compression;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace TransformFunctions
{
    public static class HL7BatchIngest
    {
        [FunctionName("HL7BatchIngest")]
        public static void Run([BlobTrigger("hl7json/ingest/hl7batch/{name}", Connection = "StorageAccount")]Stream myBlob,
            [CosmosDB(
                databaseName:"hl7json",
                collectionName :"messages",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client, string name, ILogger log)
        {
            log.LogInformation($"Processing HL7 Batch File blob hl7json/ingest/hl7batch/{name}: \n Size: {myBlob.Length} Bytes");
            int r = 0;
            if (name.ToLower().EndsWith(".tar.gz"))
            {
                r = ExtractTarGz(myBlob, client, log, name).Result;
            } else if (name.ToLower().EndsWith(".tar"))
            {
                //r = ExtractTar(myBlob, client, log).Result;
            } else
            {
                r = processUncompressedFile(myBlob, client, log, name).Result;
            }
            log.LogInformation($"Finished processing messages from HL7 Batch blob hl7json/ingest/hl7batch/{name}");
            
        }
        private static async Task<int> processUncompressedFile(Stream stream,DocumentClient client, ILogger log,string name)
        {
            string line;
            int total = 0;
            StringBuilder message = new StringBuilder();
            try
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("MSH") && message.Length > 0)
                        {
                            int removetype = name.LastIndexOf(".");
                            if (removetype < 0) removetype = name.Length;
                            await processMessage(message.ToString(), client, log, name.Substring(0, removetype));
                            total++;
                            message.Clear();
                        }
                        message.Append(line);
                        message.Append("\r");

                    }
                    if (message.Length > 0)
                    {
                        int removetype = name.LastIndexOf(".");
                        if (removetype < 0) removetype = name.Length;
                        await processMessage(message.ToString(), client, log, name.Substring(0, removetype));
                        total++;
                    }
                }
                 return total;
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                return total;
            }
        }
        private static async Task<int> ExtractTarGz(Stream stream, DocumentClient client, ILogger logger, string name)
        {
            int total = 0, errors = 0;
            logger.LogInformation($"Decompressing and extracting files from {name}...");

            // A GZipStream is not seekable, so copy it first to a FileStream
            using (var sourceStream = new GZipInputStream(stream))
            {
                using (TarInputStream tarIn = new TarInputStream(sourceStream))
                {
                    TarEntry tarEntry;
                    while ((tarEntry = tarIn.GetNextEntry()) != null)
                    {
                        if (tarEntry.IsDirectory)
                            continue;
                        byte[] bytes = null;
                        var str = new MemoryStream();
                        tarIn.CopyEntryContents(str);
                        bytes = str.ToArray();
                        var rslt = await processMessage(Encoding.UTF8.GetString(bytes), client, logger, tarEntry.Name);
                        total++;
                        if (!rslt)
                        {
                            errors++;
                            logger.LogError("Unable to process file: " + tarEntry.Name + " un-supported format!");
                        }
                        if (total % 1000 == 0) logger.LogInformation($"Processed {total} files. Errors: {errors}");
                    }
                }
            }
            logger.LogInformation($"Processed {total} files. Errors: {errors}");
            return total;
        }
        
       
        private static async Task<bool> processMessage(string message, DocumentClient client,ILogger logger,string id=null)
        {
           //if (true) return true;
            if (string.IsNullOrEmpty(message)) return false;
            string coid = (id ?? Guid.NewGuid().ToString());
            int dirend = coid.LastIndexOf("/");
            if (dirend > -1) coid = coid.Substring(dirend+1);
            int extbegin = coid.LastIndexOf(".");
            if (extbegin > -1) coid = coid.Substring(0, extbegin);
            JObject jobj = null;
            //var metadata = HL7MetaDataLoader.Instance.GetMetaDataFromMessage(message);
            try
            {
                jobj = HL7ToXmlConverter.ConvertToJObject(message);
                if (jobj == null) return false;
                string rhm = determinerhm(jobj);
                jobj["id"] = coid;
                jobj["rhm"] = rhm;
                var inserted = await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri("hl7json", "messages"), jobj);
                logger.LogTrace($"Message id {coid} from {rhm} added to Database");
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return false;
            }
        }
        private static string determinerhm(JObject obj)
        {
            if (obj==null || obj["hl7message"] == null || obj["hl7message"]["MSH"]==null) return "";
            string instance = Utilities.getFirstField(obj["hl7message"]["MSH"]["MSH.3"]);
            string source = Utilities.getFirstField(obj["hl7message"]["MSH"]["MSH.4"]);
            return (instance != null ? instance + "-" + (source ?? "") : "");
        }
    }
}
