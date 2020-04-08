using System;
using System.Threading.Tasks;

namespace CommunicationApi.Interfaces
{
    public interface IBoxStore
    {
        Task<Models.ActivatedDevice> Get(string boxId);
        Task Add(string boxId, Models.ActivatedDevice activatedDevice);
        Task<string> Activate(string activationCode, string userName, string userPhone);
        Task UpdateLastConnectedDateTime(string boxId);
        Task<DateTime> GetLastConnectedDateTime(string boxId);
    }
}