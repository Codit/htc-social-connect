using System.Threading.Tasks;
using CommunicationApi.Models;

namespace CommunicationApi.Interfaces
{
    public interface IMessageAuditStore
    {
        Task AuditMessage(WhatsappMessage message);
    }
}