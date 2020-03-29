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
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;

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
        private static readonly HttpClient httpClient = new HttpClient();

        private const string blobContainerName = "test";
        private const string tableContainerName = "textmessages";

        private string boxId;
        private string activationCode;
        private string boxStatus;

        public string BoxId { get => boxId; set => boxId = value; }
        public string ActivationCode { get => activationCode; set => activationCode = value; }
        public string BoxStatus { get => boxStatus; set => boxStatus = value; }

        public IndexModel(ISecretProvider secretProvider)
        {
            _secretProvider = secretProvider;

        }

        private async Task<JObject> RegisterNewBox()
        {
            var response = await httpClient.PostAsync("https://codit-htc.azurewebsites.net/api/v1/box/new", null);
            var responseString = JObject.Parse(await response.Content.ReadAsStringAsync());
            return responseString;
        }

        private async Task<JObject> GetBoxStatus()
        {
            
            var response = await httpClient.GetAsync("https://codit-htc.azurewebsites.net/api/v1/box/status?boxid="+BoxId);
            var responseString = JObject.Parse(await response.Content.ReadAsStringAsync());
            return responseString;
        }

        public async Task OnGet()
        {
            var apiKey = await _secretProvider.GetSecretAsync("HTC-API-Key");
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey.Value);
            //read cookie from Request object  

            string cookieBoxId = Request.Cookies["boxId"];
            string cookieActivationCode = Request.Cookies["activationCode"];

            if (Request.Query.ContainsKey("boxId"))
            {
                // The user is asking/forcing to connect to a different (existing!) box, so we override this
                cookieBoxId = Request.Query["boxId"];
            }
            
            

            if (string.IsNullOrEmpty( cookieBoxId ))
            {
                JObject responseString = await RegisterNewBox();

                var respBoxId = responseString.GetValue("boxId").ToString();
                var respActivationCode = responseString.GetValue("activationCode").ToString();
                var respBoxStatus = responseString.GetValue("status").ToString();

                Response.Cookies.Append("boxId", respBoxId);
                Response.Cookies.Append("activationCode", respActivationCode);

                BoxId = respBoxId;
                ActivationCode = respActivationCode;
                BoxStatus = respBoxStatus;
            }
            else
            {
                BoxId = cookieBoxId;
                ActivationCode = cookieActivationCode;
                BoxStatus = (await GetBoxStatus()).GetValue("status").ToString();
            }
        }

        public async Task<List<string>> GetBlobUris()
        {
            List<string> blobUris = new List<string>();
            var response = await httpClient.GetAsync("https://codit-htc.azurewebsites.net/api/v1/content/images?boxId=" + BoxId);
            var responseArray = JArray.Parse(await response.Content.ReadAsStringAsync());
                       
            foreach (JObject item in responseArray)
            {
                blobUris.Add(item.GetValue("mediaUrl").ToString() + sasContainerToken);
            }

            return blobUris;
        }

        public async Task<List<string>> GetTexts()
        {
            List<string> texts = new List<string>();
            var response = await httpClient.GetAsync("https://codit-htc.azurewebsites.net/api/v1/content/messages?boxId=" + BoxId);
            var responseArray = JArray.Parse(await response.Content.ReadAsStringAsync());
           
            foreach (JObject item in responseArray)
            {
                texts.Add(item.GetValue("text").ToString() + sasContainerToken);
            }

           
            return texts;
        }

        public async Task<string> getRandomBlubUri()
        {
            List<string> blobUris = await GetBlobUris();
            string returnString = "";
            if (blobUris.Count > 0)
            {
                Random rnd = new Random();
                int rnd_blobID = rnd.Next(0, blobUris.Count);
                returnString = blobUris[rnd_blobID];
            }
            else
            {
                returnString = "NO BLOBS";
            }
            return returnString;
        }

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
}
