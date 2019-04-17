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
using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace TransformFunctions
{
  
    public sealed class HL7MetaDataLoader
    {
        private static HL7MetaDataLoader instance = null;
        private static readonly object padlock = new object();
        public static readonly string DEFAULT_HL7VERSION = "2.3";
        private ConcurrentDictionary<string,JObject> _mdd;
        private HL7MetaDataLoader()
        {
            _mdd = new ConcurrentDictionary<string,JObject>();
        }
        
        public async Task<string> LoadMetaDataResource(string version)
        {

            if (string.IsNullOrEmpty(version)) version = DEFAULT_HL7VERSION;

            String strorageconn = Utilities.GetEnvironmentVariable("StorageAccount");
            CloudStorageAccount storageacc = CloudStorageAccount.Parse(strorageconn);

            //Create Reference to Azure Blob
            CloudBlobClient blobClient = storageacc.CreateCloudBlobClient();

            //The next 2 lines create if not exists a container named "democontainer"
            CloudBlobContainer container = blobClient.GetContainerReference("hl7json");
            await container.CreateIfNotExistsAsync();

            CloudBlockBlob blockBlob = container.GetBlockBlobReference("metadata/v" + version + "/hl7fieldmetadata.json");
            if (await blockBlob.ExistsAsync())
            {
                return await blockBlob.DownloadTextAsync();

            }
            return null;

        }
        public JObject GetMetaDataFromMessage(string hl7message)
        {
            string[] sHL7Lines = hl7message.Split('\r');
            sHL7Lines[0] = Regex.Replace(sHL7Lines[0], @"[^ -~]", "");
            string[] fields = sHL7Lines[0].Split("|");
            if (fields.Length < 12 || !fields[0].Equals("MSH")) return null;
            var version = fields[11];
            return GetMetaDataByVersion(version);
        }
        public JObject GetMetaDataByVersion(string hl7version)
        {
                
                if (!_mdd.ContainsKey(hl7version))
                { 
                    lock (padlock)
                    {
                        if (!_mdd.ContainsKey(hl7version))
                        {
                            var s = LoadMetaDataResource(hl7version).Result;
                            _mdd[hl7version] = (s != null ? JObject.Parse(s) : null);
                        }
                    }
                }
                return _mdd[hl7version];

        }
        public static HL7MetaDataLoader Instance
        {
            get
            {
                lock(padlock)
                {
                    if (instance==null)
                    {
                        instance = new HL7MetaDataLoader();
                    }
                }
                return instance;
            }
        }
    }
}
