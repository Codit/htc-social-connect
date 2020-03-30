using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Security.Core;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Microsoft.Extensions.Logging;

namespace CommunicationApi.Services.Tablestorage
{
    public class TableTextMessageServiceProvider : TableTransmitter<TextMessage>, IMediaServiceProvider
    {
        public TableTextMessageServiceProvider(ISecretProvider secretProvider, ILogger<TableTextMessageServiceProvider> logger) :
            base("textmessages", secretProvider, logger)
        {
        }

        public MediaType SupportedType => MediaType.Text;

        public async Task<IEnumerable<MediaItem>> GetItems(string tenantId)
        {
            var messages = await base.GetItems(tenantId);
            return messages.Select(ParseTextMessage);
        }

        private MediaItem ParseTextMessage(TextMessage message)
        {
            return new MediaItem
            {
                Text = message.Message,
                MediaType = MediaType.Text,
                UserName = message.From,
                Timestamp = message.ExpirationTime,
                MediaUrl = null
            };
        }

        public Task PersistMediaFile(UserInfo userInfo, string mediaUrl)
        {
            throw new NotImplementedException();
        }

        public async Task PersistTextMessage(UserInfo userInfo, TextMessage message)
        {
            await Insert(message, userInfo.BoxInfo.BoxId, Guid.NewGuid().ToString("N"));
        }
    }
}