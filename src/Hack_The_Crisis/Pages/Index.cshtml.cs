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
using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace Hack_The_Crisis.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ISecretProvider _secretProvider;


      
        private static readonly HttpClient httpClient = new HttpClient();


        private string boxId;
        private string activationCode;
        private string boxStatus;

        public string BoxId { get => boxId; set => boxId = value; }
        public string ActivationCode { get => activationCode; set => activationCode = value; }
        public string BoxStatus { get => boxStatus; set => boxStatus = value; }

        private static int initBlobCount;
        private static int initTxtCount;

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
        public  JsonResult OnGetGetData()
        {
           
            return new JsonResult(initBlobCount+";"+ initTxtCount);
        }
        public async Task OnGet()
        {
            initBlobCount = -1;
            initTxtCount = -1;
            var apiKey = await _secretProvider.GetSecretAsync("HTC-API-Key");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey.Value);
            
            var autoEvent = new AutoResetEvent(false);
            Timer _tm = new Timer(refreshData, autoEvent, 5000, 5000);

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

        private async void refreshData(object state)
        {
            await this.GetBlobUris();
            await this.GetTexts();
        }

        public async Task<List<string>> GetBlobUris()
        {
            List<string> blobUris = new List<string>();
            var response = await httpClient.GetAsync("https://codit-htc.azurewebsites.net/api/v1/content/images?boxId=" + BoxId);
            var responseArray = JArray.Parse(await response.Content.ReadAsStringAsync());
                       
            foreach (JObject item in responseArray)
            {
                blobUris.Add(item.GetValue("mediaUrl").ToString() );
            }
           
                initBlobCount = blobUris.Count;
            
            return blobUris;
        }

        public async Task<List<string>> GetTexts()
        {
            List<string> texts = new List<string>();
            var response = await httpClient.GetAsync("https://codit-htc.azurewebsites.net/api/v1/content/messages?boxId=" + BoxId);
            var responseArray = JArray.Parse(await response.Content.ReadAsStringAsync());
           
            foreach (JObject item in responseArray)
            {

                DateTime timeStamp = DateTime.Parse(item.GetValue("timestamp").ToString() );
                var dateTimeStr = timeStamp.ToString("HH:mm");
                var userName = item.GetValue("userName").ToString();
                var msgTxt = item.GetValue("text").ToString() ;
                texts.Add("[ "+dateTimeStr+" ] - "+userName +" - "+msgTxt );

            }

           
                initTxtCount = texts.Count;
            
            return texts;
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
