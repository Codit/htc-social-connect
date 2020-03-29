using System.Threading.Tasks;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunicationApi.Services.Tablestorage
{
    public class TableMessageAuditStore : TableTransmitter<WhatsappMessage>, IMessageAuditStore
    {
        public TableMessageAuditStore(IOptionsMonitor<StorageSettings> settings, ILogger<TableMessageAuditStore> logger) : 
            base("messageauditlog", settings, logger)
        {
        }

        public async Task AuditMessage(WhatsappMessage message)
        {
            await Insert(message, message.Sender, message.MessageId);
        }
    }
}