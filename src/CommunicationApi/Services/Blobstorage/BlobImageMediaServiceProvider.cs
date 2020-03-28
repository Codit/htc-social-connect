using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Flurl.Http;
using GuardNet;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CommunicationApi.Services.Blobstorage
{
    public class BlobImageMediaServiceProvider : IMediaServiceProvider
    {
        private BlobServiceClient _blobServiceClient;
        private StorageSettings _storageSettings;
        private UserDelegationKey _delegationKey;

        public BlobImageMediaServiceProvider(IOptions<StorageSettings> storageSettings)
        {
            Guard.NotNull(storageSettings.Value, nameof(storageSettings));
            _storageSettings = storageSettings.Value;
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
            var container = StorageClient.GetBlobContainerClient(userInfo.TenantInfo.Name.ToLower());
            await container.CreateIfNotExistsAsync(PublicAccessType.None);

            // TODO : check extension based on media type
            string extension = "jpg";
            await container.UploadBlobAsync(Guid.NewGuid().ToString("N") + "." + extension, mediaStream);
        }

        public Task PersistTextMessage(UserInfo userInfo, TextMessage message)
        {
            throw new NotImplementedException();
        }
        
        private async Task<string> GenerateSasUri(BlobItem blob, string tenant)
        {
            // Create a SAS token that's valid for one hour.
            var sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = tenant,
                BlobName = blob.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Specify read permissions for the SAS.
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Use the key to get the SAS token.
            if (_delegationKey == null)
            {
                _delegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddDays(7));
            }
            var sasToken = sasBuilder.ToSasQueryParameters(_delegationKey, _storageSettings.AccountName).ToString();

            // Construct the full URI, including the SAS token.
            var fullUri = new UriBuilder()
            {
                Scheme = "https",
                Host = $"{_storageSettings.AccountName}.blob.core.windows.net",
                Path = $"{tenant}/{blob.Name}",
                Query = sasToken
            };
            return fullUri.ToString();
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
            mediaItem.UserName = blobItem.Metadata.ContainsKey("user") ? blobItem.Metadata["user"] : "Onbekend";
            return mediaItem;
        }


    }
}