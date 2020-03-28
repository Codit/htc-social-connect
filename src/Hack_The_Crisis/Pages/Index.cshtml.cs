using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using Arcus.Security.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Html;
using Microsoft.WindowsAzure.Storage.Table;

namespace Hack_The_Crisis.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ISecretProvider _secretProvider;

        private static CloudStorageAccount cloudStorageAccount;
        private static CloudBlobClient blobClient;
        private static CloudTableClient tableClient;
        private static CloudBlobContainer blobContainer;
        private static CloudTable tableContainer;
        private static string sasContainerToken;


        private const string blobContainerName = "images";
        private string defaultLanguage = "nl";
        private const string tableContainerName = "textmessages";
        public string Message { get; set; }

        public IndexModel(ISecretProvider secretProvider)
        {
            _secretProvider = secretProvider;
        }

        public async Task OnGet()
        {
            var connectionStringSecret = await _secretProvider.GetSecretAsync("HTC-Storage-Connectionstring");
            cloudStorageAccount = CloudStorageAccount.Parse(connectionStringSecret.Value);
            blobClient = cloudStorageAccount.CreateCloudBlobClient();
            tableClient = cloudStorageAccount.CreateCloudTableClient();

            string language = this.Request.Query["lang"]; 
            if (!string.IsNullOrEmpty(language)) defaultLanguage = language;
            blobContainer = blobClient.GetContainerReference(blobContainerName+ defaultLanguage);
            tableContainer = tableClient.GetTableReference(tableContainerName);
        }

        public async Task<List<imageForLetter>> getBlobUris()
        {
            List<IListBlobItem> list = new List<IListBlobItem>();
            List<imageForLetter> lstImageForLetter = new List<imageForLetter>();

            BlobResultSegment segment = await blobContainer.ListBlobsSegmentedAsync(null);
            list.AddRange(segment.Results);
            while (segment.ContinuationToken != null)
            {
                segment = await blobContainer.ListBlobsSegmentedAsync(segment.ContinuationToken);
                list.AddRange(segment.Results);
            }

            if (sasContainerToken == null)
            {
                SharedAccessBlobPolicy adHocPolicy = new SharedAccessBlobPolicy()
                {
                    // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request.
                    // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                    Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List
                };
                sasContainerToken = blobContainer.GetSharedAccessSignature(adHocPolicy, null);
            }

            foreach (IListBlobItem item in list)
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    //blobUris.Add(blob.Uri + sasContainerToken);

                    imageForLetter il = new imageForLetter();
                    il.letter = item.Uri.LocalPath.Replace("/" + blobContainerName + defaultLanguage + "/", "").Replace(".png", "").ToUpper();
                    il.url = blob.Uri + sasContainerToken;
                    lstImageForLetter.Add(il);

                }
                else if (item.GetType() == typeof(CloudPageBlob))
                {
                    CloudPageBlob blob = (CloudPageBlob)item;
                    //blobUris.Add(blob.Uri + sasContainerToken);
                }
                else if (item.GetType() == typeof(CloudBlobDirectory))
                {
                    CloudBlobDirectory dir = (CloudBlobDirectory)item;
                    //blobUris.Add(dir.Uri + sasContainerToken);
                }
            }

            //TODO: call API and filter lstImageForLetter

            return lstImageForLetter;
        }

        public async Task<List<string>> getTexts()
        {
            List<DynamicTableEntity> list = new List<DynamicTableEntity>();
            TableContinuationToken token = null;
            List<string> texts = new List<string>();

            do
            {
                TableQuerySegment queryResult = await tableContainer.ExecuteQuerySegmentedAsync(new TableQuery(), token);
                list.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);


            foreach (DynamicTableEntity item in list)
            {
                texts.Add("[" + item.Timestamp.DateTime + "]: " + item.Properties["From"].StringValue + " - " + item.Properties["Message"].StringValue);
            }

            return texts;
        }

        //public async Task<string> getRandomBlubUri()
        //{
        //    List<string> blobUris = await getBlobUris();
        //    string returnString = "";
        //    if (blobUris.Count > 0)
        //    {
        //        Random rnd = new Random();
        //        int rnd_blobID = rnd.Next(0, blobUris.Count);
        //        returnString = blobUris[rnd_blobID];
        //    }
        //    else
        //    {
        //        returnString = "NO BLOBS";
        //    }
        //    return returnString;
        //}

        public static class JavaScriptConvert
        {
            public static HtmlString SerializeObject(object value)
            {
                using (var stringWriter = new StringWriter())
                using (var jsonWriter = new JsonTextWriter(stringWriter))
                {
                    var serializer = new JsonSerializer
                    {
                        // Let's use camelCasing as is common practice in JavaScript
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };

                    // We don't want quotes around object names
                    jsonWriter.QuoteName = false;
                    serializer.Serialize(jsonWriter, value);

                    return new HtmlString(stringWriter.ToString());
                }
            }
        }
    }

    public class imageForLetter
    {
        public string url;
        public string letter;
    }
}
