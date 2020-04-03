using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Hack_The_Crisis.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ISecretProvider _secretProvider;

        private static readonly HttpClient HttpClient = new HttpClient();
        private string ApiUrl => _configuration["API_URL"];
        public string BoxId { get; set; }
        public string ActivationCode { get; set; }
        public string BoxStatus { get; set; }

        private static int initBlobCount;
        private static int initTxtCount;

        public IndexModel(ISecretProvider secretProvider, IConfiguration configuration)
        {
            _configuration = configuration;
            _secretProvider = secretProvider;
        }

        private async Task<JObject> RegisterNewBox()
        {
            var response = await HttpClient.PostAsync($"{ApiUrl}/api/v1/box/new", null);
            var responseString = JObject.Parse(await response.Content.ReadAsStringAsync());
            return responseString;
        }

        private async Task<JObject> GetBoxStatus()
        {
            var response =
                await HttpClient.GetAsync($"{ApiUrl}/api/v1/box/status?boxid=" + BoxId);
            var responseString = JObject.Parse(await response.Content.ReadAsStringAsync());
            return responseString;
        }

        public JsonResult OnGetGetData()
        {
            return new JsonResult(initBlobCount + ";" + initTxtCount);
        }

        public async Task OnGet()
        {
            initBlobCount = -1;
            initTxtCount = -1;
            var apiKey = await _secretProvider.GetSecretAsync("HTC-API-Key");
            HttpClient.DefaultRequestHeaders.Clear();
            HttpClient.DefaultRequestHeaders.Add("x-api-key", apiKey.Value);

            var autoEvent = new AutoResetEvent(false);
            Timer _tm = new Timer(refreshData, autoEvent, 20000, 20000);

            //read cookie from Request object  

            string cookieBoxId = Request.Cookies["boxId"];
            string cookieActivationCode = Request.Cookies["activationCode"];

            if (Request.Query.ContainsKey("boxId"))
                // The user is asking/forcing to connect to a different (existing!) box, so we override this
                cookieBoxId = Request.Query["boxId"];


            if (string.IsNullOrEmpty(cookieBoxId))
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
            await GetBlobUris();
            await GetTexts();
        }

        public async Task<List<string>> GetBlobUris()
        {
            List<string> blobUris = new List<string>();
            var response =
                await HttpClient.GetAsync($"{ApiUrl}/api/v1/content/images?boxId=" + BoxId);
            var responseArray = JArray.Parse(await response.Content.ReadAsStringAsync());

            foreach (JObject item in responseArray) blobUris.Add(item.GetValue("mediaUrl").ToString());

            initBlobCount = blobUris.Count;

            return blobUris;
        }

        public async Task<List<string>> GetTexts()
        {
            List<string> texts = new List<string>();
            var response =
                await HttpClient.GetAsync($"{ApiUrl}/api/v1/content/messages?boxId=" + BoxId);
            var responseArray = JArray.Parse(await response.Content.ReadAsStringAsync());

            foreach (JObject item in responseArray)
            {
                DateTime timeStamp = DateTime.Parse(item.GetValue("timestamp").ToString());
                var dateTimeStr = timeStamp.ToString("HH:mm");
                var userName = item.GetValue("userName").ToString();
                var msgTxt = item.GetValue("text").ToString();
                texts.Add("[ " + dateTimeStr + " ] - " + userName + " - " + msgTxt);
            }

            initTxtCount = texts.Count;

            return texts;
        }
    }
}