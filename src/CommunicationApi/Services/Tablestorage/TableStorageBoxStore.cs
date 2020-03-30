using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Security.Core;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Microsoft.Extensions.Logging;

namespace CommunicationApi.Services.Tablestorage
{
    public class TableStorageBoxStore : TableTransmitter<ActivatedDevice>, IBoxStore
    {
        private const string TableName = "boxes";
        private const string PartitionKey = "devices";

        public TableStorageBoxStore(ISecretProvider secretProvider, ILogger<TableStorageBoxStore> logger)
            : base(TableName, secretProvider, logger)
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

        public async Task<string> Activate(string activationCode, string userName, string userPhone)
        {
            var devices = await GetItems(PartitionKey);
            var device = devices.FirstOrDefault(d =>
                d.ActivationCode.Equals(activationCode, StringComparison.InvariantCultureIgnoreCase));
            if (device != null)
            {
                device.Status = BoxStatus.Activated;
                device.AdminUserName = userName;
                device.AdminUserPhone = userPhone;
                await Upsert(device, PartitionKey, device.BoxId);
                return device.BoxId;
            }

            return null;
        }
    }
}