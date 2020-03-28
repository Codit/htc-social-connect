using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationApi.Models;

namespace CommunicationApi.Interfaces
{
    public interface IMediaServiceProvider
    {
        MediaType SupportedType { get; }
        Task<IEnumerable<MediaItem>> GetItems(string tenantId);
        Task PersistMediaFile(UserInfo userInfo, string mediaUrl);
        Task PersistTextMessage(UserInfo userInfo, TextMessage message);
    }
}