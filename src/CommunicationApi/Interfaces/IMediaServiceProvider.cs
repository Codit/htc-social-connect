using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationApi.Models;

namespace CommunicationApi.Interfaces
{
    public interface IMediaServiceProvider
    {
        Task<IEnumerable<MediaItem>> GetItems(string tenantId);
    }
}