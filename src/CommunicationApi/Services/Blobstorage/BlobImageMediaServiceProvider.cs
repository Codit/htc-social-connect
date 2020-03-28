using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Flurl.Http;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CommunicationApi.Services.Blobstorage
{
    public class BlobImageMediaServiceProvider : IMediaServiceProvider
    {
        private BlobServiceClient _blobServiceClient;
        private StorageSettings _storageSettings;
        private UserDelegationKey _delegationKey;
        private ILogger<BlobImageMediaServiceProvider> _logger;

        public BlobImageMediaServiceProvider(IOptions<StorageSettings> storageSettings,
            ILogger<BlobImageMediaServiceProvider> logger)
        {
            Guard.NotNull(storageSettings.Value, nameof(storageSettings));
            _storageSettings = storageSettings.Value;
            _logger = logger;
        }

        public MediaType SupportedType => MediaType.Image;

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

        public async Task<IEnumerable<MediaItem>> GetItems(string tenantId)
        {
            var blobContainer = StorageClient.GetBlobContainerClient(tenantId);

            var blobs = blobContainer.GetBlobs();

            var response = new List<MediaItem>();
            foreach (var blobItem in blobs)
            {
                response.Add(await GetMediaItem(tenantId, blobItem));
            }

            return response;
        }

        public async Task PersistMediaFile(UserInfo userInfo, string mediaUrl)
        {
            // download image to stream
            var mediaStream = await mediaUrl.GetStreamAsync(completionOption: HttpCompletionOption.ResponseContentRead);

            // Create the container and return a container client object
            var container = StorageClient.GetBlobContainerClient(userInfo.BoxInfo.BoxId.ToLower());
            await container.CreateIfNotExistsAsync(PublicAccessType.None);

            // TODO : check extension based on media type
            string extension = "jpg";
            var blob = await container.UploadBlobAsync(Guid.NewGuid().ToString("N") + "." + extension, mediaStream);
            
        }

        public Task PersistTextMessage(UserInfo userInfo, TextMessage message)
        {
            throw new NotImplementedException();
        }

        private async Task<string> GenerateSasUri(BlobItem blob, string tenant)
        {
            try
            {
                //  Defines the resource being accessed and for how long the access is allowed.
                var blobSasBuilder = new BlobSasBuilder
                {
                    StartsOn = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(15d)),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                    BlobContainerName = tenant,
                    Resource = "b",
                    BlobName = blob.Name,
                };

                //  Defines the type of permission.
                blobSasBuilder.SetPermissions(BlobSasPermissions.Read);

                //  Builds an instance of StorageSharedKeyCredential      
                var storageSharedKeyCredential =
                    new StorageSharedKeyCredential(_storageSettings.AccountName, _storageSettings.AccountKey);

                //  Builds the Sas URI.
                var sasQueryParameters =
                    blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential);


                // Construct the full URI, including the SAS token.
                var fullUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = $"{_storageSettings.AccountName}.blob.core.windows.net",
                    Path = $"{tenant}/{blob.Name}",
                    Query = sasQueryParameters.ToString()
                };
                return fullUri.ToString();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error occurred while generating the Sas Uri for blob {blob.Name}");
                throw;
            }
        }


        private async Task<MediaItem> GetMediaItem(string tenantId, BlobItem blobItem)
        {
            var mediaItem = new MediaItem
            {
                MediaUrl = await GenerateSasUri(blobItem, tenantId),
                MediaType = MediaType.Image
            };
            if (blobItem.Properties.LastModified != null)
                mediaItem.Timestamp = blobItem.Properties.LastModified.Value;
            if(blobItem.Metadata!=null)
                mediaItem.UserName = blobItem.Metadata.ContainsKey("user") ? blobItem.Metadata["user"] : "Onbekend";
            return mediaItem;
        }
    }
}