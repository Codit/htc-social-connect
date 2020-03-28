using System.Threading.Tasks;
using CommunicationApi.Contracts.v1;

namespace CommunicationApi.Interfaces
{
    public interface IBoxStore
    {
        Task<ActivatedDevice> Get(string boxId);
        Task Add(string boxId, ActivatedDevice activatedDevice);
    }
}