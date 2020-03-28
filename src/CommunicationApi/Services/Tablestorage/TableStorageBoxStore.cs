using System.Threading.Tasks;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunicationApi.Services.Tablestorage
{
    public class TableStorageBoxStore : TableTransmitter<ActivatedDevice>, IBoxStore
    {
        private const string TableName = "boxes";
        private const string PartitionKey = "devices";

        public TableStorageBoxStore(IOptionsMonitor<StorageSettings> settings, ILogger<TableStorageBoxStore> logger)
            : base(TableName, settings, logger)
        {
        }

        public async Task<ActivatedDevice> Get(string boxId)
        {
            var activatedDevice = await GetItem(PartitionKey, boxId);
            return activatedDevice;
        }

        public async Task Add(string boxId, ActivatedDevice activatedDevice)
        {
            await Insert(activatedDevice, PartitionKey, boxId);
        }
    }
}