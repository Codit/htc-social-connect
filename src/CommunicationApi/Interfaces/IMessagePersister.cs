using System.Threading.Tasks;
using CommunicationApi.Models;

namespace CommunicationApi.Interfaces
{
    public interface IMessagePersister
    {
        Task PersistMessage(TextMessage message, UserInfo userInfo);
    }
}