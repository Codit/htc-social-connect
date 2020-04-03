using System.Threading.Tasks;
using Arcus.Security.Core;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Microsoft.Extensions.Logging;

namespace CommunicationApi.Services.Tablestorage
{
    public class TableMessageAuditStore : TableTransmitter<WhatsappMessage>, IMessageAuditStore
    {
        public TableMessageAuditStore(ISecretProvider secretProvider, ILogger<TableMessageAuditStore> logger) : 
            base("messageauditlog", secretProvider, logger)
        {
        }

        public async Task AuditMessage(WhatsappMessage message)
        {
            await Insert(message, message.Sender, message.MessageId);
        }
    }
}