using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationApi.Contracts.v1;
using CommunicationApi.Interfaces;
using Microsoft.Extensions.Logging;

namespace CommunicationApi.Services
{
    public class HardcodedBoxStore : IBoxStore
    {
        private readonly Dictionary<string, ActivatedDevice> _registeredBoxes = new Dictionary<string, ActivatedDevice>();
        private readonly ILogger<HardcodedBoxStore> _logger;

        public HardcodedBoxStore(ILogger<HardcodedBoxStore> logger)
        {
            _logger = logger;
        }

        public Task<ActivatedDevice> Get(string boxId)
        {
            if (_registeredBoxes.ContainsKey(boxId))
            {
                return Task.FromResult(_registeredBoxes[boxId]);
            }

            return Task.FromResult((ActivatedDevice)null);
        }

        public Task Add(string boxId, ActivatedDevice activatedDevice)
        {
            _registeredBoxes.Add(boxId, activatedDevice);

            _logger.LogEvent("New Box Registered", new Dictionary<string, object> { { "BoxId", activatedDevice.BoxId } });

            return Task.CompletedTask;
        }
    }
}
