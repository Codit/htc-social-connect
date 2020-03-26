using System;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Flurl.Http;
using GuardNet;
using Microsoft.Extensions.Options;

namespace CommunicationApi.Services.Blobstorage
{
    public class BlobMediaPersister : IMediaPersister
    {
        private BlobServiceClient _blobServiceClient;
        private StorageSettings _storageSettings;

        public BlobMediaPersister(IOptions<StorageSettings> storageSettings)
        {
            Guard.NotNull(storageSettings.Value, nameof(storageSettings));
            _storageSettings = storageSettings.Value;
        }

        private BlobServiceClient StorageClient
        {
            get
            {
                if (_blobServiceClient == null)
                {
                    string cnxstring =
                        $"DefaultEndpointsProtocol=https;AccountName={_storageSettings.AccountName};AccountKey={_storageSettings.AccountKey};EndpointSuffix=core.windows.net";
                    _blobServiceClient = new BlobServiceClient(cnxstring);
                }

                return _blobServiceClient;
            }
        }

        public async Task PersistMediaFile(UserInfo userInfo, string mediaUrl)
        {
            // download image to stream
            var mediaStream = await mediaUrl.GetStreamAsync(completionOption: HttpCompletionOption.ResponseContentRead);

            // Create the container and return a container client object
            var container = StorageClient.GetBlobContainerClient(userInfo.TenantInfo.Name.ToLower());
            await container.CreateIfNotExistsAsync(PublicAccessType.None);
            // TODO : check extension based on media type
            string extension = "jpg";
            await container.UploadBlobAsync(Guid.NewGuid().ToString("N") + "." + extension, mediaStream);
        }
    }
}