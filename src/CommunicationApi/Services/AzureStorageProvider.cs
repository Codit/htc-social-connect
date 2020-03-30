using System.Threading.Tasks;
using Arcus.Security.Core;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;

namespace CommunicationApi.Services
{
    public class AzureStorageProvider
    {
        private CloudStorageAccount _storageAccount;
        private BlobServiceClient _blobServiceClient;

        protected ILogger Logger { get; set; }
        protected ISecretProvider SecretProvider { get; }

        protected AzureStorageProvider(ISecretProvider secretProvider, ILogger logger)
        {
            SecretProvider = secretProvider;
            Logger = logger;
        }

        protected async Task<CloudStorageAccount> GetStorageAccount()
        {
            if (_storageAccount == null)
            {
                var storageConnectionString = await GetConnectionString();
                _storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }

            return _storageAccount;
        }

        protected async Task<BlobServiceClient> GetBlobStorageClient()
        {
            if (_blobServiceClient == null)
            {
                var storageConnectionString = await GetConnectionString();
                _blobServiceClient = new BlobServiceClient(storageConnectionString);
            }

            return _blobServiceClient;
        }

        private async Task<string> GetConnectionString()
        {
            var storageConnectionString = await SecretProvider.GetRawSecretAsync("HTC-Storage-Connectionstring");
            return storageConnectionString;
        }

        protected async Task<string> GetStorageKey()
        {
            var storageConnectionString = await SecretProvider.GetRawSecretAsync("HTC-Storage-Key");
            return storageConnectionString;
        }
    }
}
