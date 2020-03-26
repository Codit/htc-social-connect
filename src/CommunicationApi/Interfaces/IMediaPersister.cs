using System.Threading.Tasks;
using CommunicationApi.Models;

namespace CommunicationApi.Interfaces
{
    public interface IMediaPersister
    {
        Task PersistMediaFile(UserInfo userInfo, string mediaUrl);
    }
}