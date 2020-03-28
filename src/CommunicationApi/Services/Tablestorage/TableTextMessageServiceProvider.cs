using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Microsoft.Extensions.Options;

namespace CommunicationApi.Services.Tablestorage
{
    public class TableTextMessageServiceProvider : TableTransmitter<TextMessage>, IMediaServiceProvider
    {
        public TableTextMessageServiceProvider(IOptions<StorageSettings> settings) :
            base(settings.Value, "textmessages")
        {
        }

        public MediaType SupportedType => MediaType.Text;

        public Task<IEnumerable<MediaItem>> GetItems(string tenantId)
        {
            // TODO : joachim
            throw new NotImplementedException();
        }

        public Task PersistMediaFile(UserInfo userInfo, string mediaUrl)
        {
            throw new NotImplementedException();
        }

        public async Task PersistTextMessage(UserInfo userInfo, TextMessage message)
        {
            await Insert(message, userInfo.TenantInfo.Name, Guid.NewGuid().ToString("N"));
        }
    }
}