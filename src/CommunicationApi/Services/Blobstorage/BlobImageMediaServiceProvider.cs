using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Flurl.Http;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace CommunicationApi.Services.Blobstorage
{
    public class BlobImageMediaServiceProvider : IMediaServiceProvider
    {
        private readonly ISecretProvider _secretProvider;
        private BlobServiceClient _blobServiceClient;
        private StorageSettings _storageSettings;
        private ILogger<BlobImageMediaServiceProvider> _logger;

        public BlobImageMediaServiceProvider(ISecretProvider secretProvider, ILogger<BlobImageMediaServiceProvider> logger)
        {
            Guard.NotNull(secretProvider, nameof(secretProvider));

            _logger = logger;
            _secretProvider = secretProvider;
        }

        public MediaType SupportedType => MediaType.Image;

        private async Task<BlobServiceClient> GetStorageClient()
        {
            if (_blobServiceClient == null)
            {
                var storageConnectionString = await _secretProvider.GetRawSecretAsync("HTC-Storage-Connectionstring");
                _blobServiceClient = new BlobServiceClient(storageConnectionString);
            }

            return _blobServiceClient;
        }

        public async Task<IEnumerable<MediaItem>> GetItems(string tenantId)
        {
            var response = new List<MediaItem>();
            var storageClient = await GetStorageClient();
            var blobContainer = storageClient.GetBlobContainerClient(tenantId);
            if (await blobContainer.ExistsAsync())
            {
                var blobs = blobContainer.GetBlobs(BlobTraits.Metadata);

                foreach (var blobItem in blobs)
                {
                    response.Add(await GetMediaItem(tenantId, blobItem));
                }
            }

            if (response.Count == 0)
            {
                _logger.LogTrace("List of images in blob is empty, return default images");
                response.Add(new MediaItem { MediaType = MediaType.Image, MediaUrl = "https://codithtc.blob.core.windows.net/public/media/default_image01.jpg", Timestamp = DateTimeOffset.UtcNow, UserName = "SocialTV" });
                response.Add(new MediaItem { MediaType = MediaType.Image, MediaUrl = "https://codithtc.blob.core.windows.net/public/media/default_image02.jpg", Timestamp = DateTimeOffset.UtcNow, UserName = "SocialTV" });
                response.Add(new MediaItem { MediaType = MediaType.Image, MediaUrl = "https://codithtc.blob.core.windows.net/public/media/default_image03.jpg", Timestamp = DateTimeOffset.UtcNow, UserName = "SocialTV" });
            }
            return response;
        }

        public async Task PersistMediaFile(UserInfo userInfo, string mediaUrl)
        {
            // download image to stream
            var mediaStream = await mediaUrl.GetStreamAsync(completionOption: HttpCompletionOption.ResponseContentRead);

            // Create the container and return a container client object
            var storageClient = await GetStorageClient();
            var container = storageClient.GetBlobContainerClient(userInfo.BoxInfo.BoxId.ToLower());
            await container.CreateIfNotExistsAsync();

            // TODO : check extension based on media type
            string extension = "jpg";
            var blobClient = container.GetBlockBlobClient(Guid.NewGuid().ToString("N") + "." + extension);
            await blobClient.UploadAsync(mediaStream, null,
                new Dictionary<string, string>
                {
                    {"user", userInfo.Name}
                });
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
            if (blobItem.Metadata != null)
                mediaItem.UserName = blobItem.Metadata.ContainsKey("user") ? blobItem.Metadata["user"] : "Onbekend";
            return mediaItem;
        }
    }
}