using System;
using System.Threading.Tasks;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Microsoft.Extensions.Options;

namespace CommunicationApi.Services.Tablestorage
{
    public class TableMessagePersister : TableTransmitter<TextMessage>, IMessagePersister
    {
        public TableMessagePersister(IOptions<StorageSettings> settings) : 
            base(settings.Value, "textmessages")
        {
        }

        public async Task PersistMessage(TextMessage message, UserInfo userInfo)
        {
            await base.Insert(message, userInfo.TenantInfo.Name, Guid.NewGuid().ToString("N"));
        }
    }
}