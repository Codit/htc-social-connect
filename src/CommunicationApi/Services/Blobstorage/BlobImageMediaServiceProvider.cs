using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Flurl.Http;
using GuardNet;
using Microsoft.Extensions.Options;

namespace CommunicationApi.Services.Blobstorage
{
    public class BlobImageMediaServiceProvider : IMediaServiceProvider
    {
        private BlobServiceClient _blobServiceClient;
        private IOptionsMonitor<StorageSettings> _storageSettings;

        public BlobImageMediaServiceProvider(IOptionsMonitor<StorageSettings> storageSettings)
        {
            Guard.NotNull(storageSettings, nameof(storageSettings));
            
            _storageSettings = storageSettings;
        }

        public MediaType SupportedType => MediaType.Image;

        private BlobServiceClient StorageClient
        {
            get
            {
                if (_blobServiceClient == null)
                {
                    var currentStorageSettings = _storageSettings.CurrentValue;
                    string cnxstring = $"DefaultEndpointsProtocol=https;AccountName={currentStorageSettings.AccountName};AccountKey={currentStorageSettings.AccountKey};EndpointSuffix=core.windows.net";
                    _blobServiceClient = new BlobServiceClient(cnxstring);
                }

                return _blobServiceClient;
            }
        }


        public Task<IEnumerable<MediaItem>> GetItems(string tenantId)
        {
            // TODO : joachim : retrieve sas tokens here	
            throw new NotImplementedException();
        }

        public async Task PersistMediaFile(UserInfo userInfo, string mediaUrl)
        {
            // download image to stream	
            var mediaStream = await mediaUrl.GetStreamAsync();

            // Create the container and return a container client object	
            var container = StorageClient.GetBlobContainerClient(userInfo.TenantInfo.Name.ToLower());
            await container.CreateIfNotExistsAsync();
            // TODO : check extension based on media type	
            string extension = "jpg";
            await container.UploadBlobAsync(Guid.NewGuid().ToString("N") + "." + extension, mediaStream);
        }

        public Task PersistTextMessage(UserInfo userInfo, TextMessage message)
        {
            throw new NotImplementedException();
        }
    }
}