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
                // If status is already Activated, no need to update the device settings
                if (device.Status != BoxStatus.Activated)
                {
                    device.Status = BoxStatus.Activated;
                    device.AdminUserName = userName;
                    device.AdminUserPhone = userPhone;
                    await Upsert(device, PartitionKey, device.BoxId);
                }
                return device.BoxId;
            }

            return null;
        }

        public async Task UpdateLastConnectedDateTime(string boxId)
        {
            var device = await Get(boxId);
            device.LastConnectedDateTime = DateTime.Now;
            await Upsert(device, PartitionKey, boxId);
        }

        public async Task<DateTime> GetLastConnectedDateTime(string boxId)
        {
            var device = await Get(boxId);
            return (device.LastConnectedDateTime);
        }
    }
}