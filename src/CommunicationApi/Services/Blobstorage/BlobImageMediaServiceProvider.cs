using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Azure.Storage;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace CommunicationApi.Services.Blobstorage
{
    public class BlobImageMediaServiceProvider : AzureStorageProvider, IMediaServiceProvider
    {
        public BlobImageMediaServiceProvider(ISecretProvider secretProvider, ILogger<BlobImageMediaServiceProvider> logger)
            : base(secretProvider, logger)
        {
        }

        public MediaType SupportedType => MediaType.Image;

        public async Task<IEnumerable<MediaItem>> GetItems(string tenantId)
        {
            var response = new List<MediaItem>();
            var storageClient = await GetBlobStorageClient();
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
                Logger.LogTrace("List of images in blob is empty, return default images");
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
            var storageClient = await GetBlobStorageClient();
            var container = storageClient.GetBlobContainerClient(userInfo.BoxInfo.BoxId.ToLower());
            await container.CreateIfNotExistsAsync();

            // TODO : check extension based on media type
            string extension = "jpg";
            var blobClient = container.GetBlockBlobClient(Guid.NewGuid().ToString("N") + "." + extension);
            await blobClient.UploadAsync(mediaStream, null,
                new Dictionary<string, string>
                {
                    {"user", Convert.ToBase64String(Encoding.UTF8.GetBytes(userInfo.Name))}
                });
        }

        public Task PersistTextMessage(UserInfo userInfo, TextMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteContent(UserInfo userInfo)
        {
            var storageClient = await GetBlobStorageClient();
            var container = storageClient.GetBlobContainerClient(userInfo.BoxInfo.BoxId.ToLower());
            await container.DeleteIfExistsAsync();
        }

        private async Task<string> GenerateSasUri(BlobItem blob, string tenant)
        {
            try
            {
                var storageAccount = await GetStorageAccount();
                var storageKey = await GetStorageKey();

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
                    new StorageSharedKeyCredential(storageAccount.Credentials.AccountName, storageKey);

                //  Builds the Sas URI.
                var sasQueryParameters =
                    blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential);


                // Construct the full URI, including the SAS token.
                var fullUri = new UriBuilder
                {
                    Scheme = "https",
                    Host = $"{storageAccount.Credentials.AccountName}.blob.core.windows.net",
                    Path = $"{tenant}/{blob.Name}",
                    Query = sasQueryParameters.ToString()
                };
                return fullUri.ToString();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"An error occurred while generating the Sas Uri for blob {blob.Name}");
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
                mediaItem.UserName = blobItem.Metadata.ContainsKey("user") ? GetMetadataValue(blobItem.Metadata["user"]) : "Onbekend";
            return mediaItem;
        }

        private string GetMetadataValue(string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch(Exception)
            {
                return value;
            }
        }
    }
}