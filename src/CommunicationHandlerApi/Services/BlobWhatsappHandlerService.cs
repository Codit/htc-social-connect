using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CommunicationHandlerApi.Extensions;
using CommunicationHandlerApi.Interfaces;
using CommunicationHandlerApi.Models;
using Flurl.Http;
using GuardNet;
using Microsoft.Extensions.Options;

namespace CommunicationHandlerApi.Services
{
    public class BlobWhatsappHandlerService : IWhatsappHandlerService
    {
        private StorageSettings _storageSettings;
        private IUserMatcher _userMatcher;
        private BlobServiceClient _blobServiceClient;

        public BlobWhatsappHandlerService(IOptions<StorageSettings> storageSettings, IUserMatcher userMatcher)
        {
            Guard.NotNull(storageSettings.Value, nameof(storageSettings));
            Guard.NotNull(userMatcher, nameof(userMatcher));
            _storageSettings = storageSettings.Value;
            _userMatcher = userMatcher;
            string cnxstring =
                $"DefaultEndpointsProtocol=https;AccountName={_storageSettings.AccountName};AccountKey={_storageSettings.AccountKey};EndpointSuffix=core.windows.net";
            _blobServiceClient = new BlobServiceClient(cnxstring);
        }

        public async Task<WhatsappResponse> ProcessRequest(Dictionary<string, string> pars)
        {
            var tenantInfo = await _userMatcher.Match(pars);
            var mediaCount = int.Parse(pars.GetParameter("NumMedia", "0"));
            if (mediaCount > 0)
            {
                return await ProcessImage(tenantInfo, pars, mediaCount);
            }
            else
            {
                return await ProcessText(tenantInfo, pars);
            }

        }

        private async Task<WhatsappResponse> ProcessText(UserInfo userInfo, Dictionary<string, string> pars)
        {
            string message = pars.GetParameter("Body");
            string userMessage = $"Bericht van {userInfo.Name}({userInfo.PhoneNumber}): {message}";
            return new WhatsappResponse{ ResponseMessage = $"We ontvingen je bericht, {userInfo.Name}"};
        }

        private async Task<WhatsappResponse> ProcessImage(UserInfo userInfo, Dictionary<string, string> pars, int mediaCount)
        {
            bool mediaFound = true;
            int currentMediaId = 0;
            while (mediaFound)
            {
                var mediaUrl = pars.GetParameter($"MediaUrl{currentMediaId}");
                mediaFound = !string.IsNullOrEmpty(mediaUrl);
                if (mediaFound)
                {
                    await PersistImage(userInfo, WebUtility.UrlDecode(mediaUrl));
                }

                currentMediaId++;
            }

            string message = $"We stuurden je foto door, {userInfo.Name}";
            if (mediaCount > 1)
            {
                message =$"We stuurden je {mediaCount} foto's door, {userInfo.Name}";
            }
            return new WhatsappResponse{ ResponseMessage = message};
        }

        private async Task PersistImage(UserInfo userInfo, string mediaUrl)
        {
            // download image to stream
            var mediaStream = await mediaUrl.GetStreamAsync(completionOption: HttpCompletionOption.ResponseContentRead);

            // Create the container and return a container client object
            var container = _blobServiceClient.GetBlobContainerClient(userInfo.TenantInfo.Name.ToLower());
            await container.CreateIfNotExistsAsync(PublicAccessType.None);
            string extension = "jpg";
            await container.UploadBlobAsync(Guid.NewGuid().ToString("N") + "." + extension, mediaStream);
        }
    }
}