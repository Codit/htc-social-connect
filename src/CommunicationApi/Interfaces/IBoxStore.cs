using System.Threading.Tasks;
using CommunicationApi.Contracts.v1;

namespace CommunicationApi.Interfaces
{
    public interface IBoxStore
    {
        Task<Models.ActivatedDevice> Get(string boxId);
        Task Add(string boxId, Models.ActivatedDevice activatedDevice);
        Task<string> Activate(string activationCode);
    }
}