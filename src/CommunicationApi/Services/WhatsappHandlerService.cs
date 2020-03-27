using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CommunicationApi.Extensions;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace CommunicationApi.Services
{
    public class WhatsappHandlerService : IWhatsappHandlerService
    {
        private IUserMatcher _userMatcher;
        private IMediaPersister _mediaPersister;
        private IMessagePersister _messagePersister;
        private ILogger<WhatsappHandlerService> _logger;

        public WhatsappHandlerService(IMessagePersister messagePersister, IMediaPersister mediaPersister,
            IUserMatcher userMatcher, ILogger<WhatsappHandlerService> logger)
        {
            Guard.NotNull(userMatcher, nameof(userMatcher));
            Guard.NotNull(messagePersister, nameof(messagePersister));
            Guard.NotNull(mediaPersister, nameof(mediaPersister));
            Guard.NotNull(logger, nameof(logger));
            _userMatcher = userMatcher;
            _mediaPersister = mediaPersister;
            _messagePersister = messagePersister;
            _logger = logger;
        }

        public async Task<WhatsappResponse> ProcessRequest(Dictionary<string, string> pars)
        {
            try
            {
                var whatsappMessage = new WhatsappMessage(pars);
                // Check in cache (or table) if phone Number is linked
                var userInfo = await _userMatcher.Match(whatsappMessage.Sender);

                if (!string.IsNullOrEmpty(userInfo?.TenantInfo?.Name))
                {
                    return await HandleTenantMessage(userInfo, whatsappMessage);
                }

                // This is for users that are not yet linked to a tenant
                return await HandleNewUserConversation(userInfo, whatsappMessage);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"An error happened while processing the incoming WhatsApp message with parameters {pars}");
                throw;
            }
        }

        private async Task<WhatsappResponse> HandleTenantMessage(UserInfo userInfo,WhatsappMessage message)
        {
            // This is handling a user that is already linked to a tenant
            _logger.LogTrace(
                $"Processing message from user {userInfo.Name} and phone nr {userInfo.PhoneNumber} in tenant {userInfo.TenantInfo.Name}");
            if (message.MediaItems.Count > 0)
            {
                return await ProcessImages(userInfo, message);
            }

            return await ProcessText(userInfo, message);
        }

        private async Task<WhatsappResponse> HandleNewUserConversation( UserInfo userInfo, WhatsappMessage message)
        {
            string command = message.MessageContent;
            switch (userInfo.ConversationState)
            {
                case ConversationState.New:
                    break;
                case ConversationState.AwaitingName:
                    break;
                case ConversationState.AwaitingActivation:
                    break;
                case ConversationState.Completed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            throw new NotImplementedException();
        }

        private async Task<WhatsappResponse> ProcessText(UserInfo userInfo, WhatsappMessage message)
        {
            string userMessage = $"Bericht van {userInfo.Name}({userInfo.PhoneNumber}): {message}";
            await _messagePersister.PersistMessage(new TextMessage
            {
                From = userInfo.Name,
                PhoneNumber = userInfo.PhoneNumber,
                Message = message.MessageContent,
                ExpirationTime = DateTimeOffset.UtcNow.AddDays(1)
            }, userInfo);

            _logger.LogEvent("New Message Received");
            _logger.LogMetric("Text Received", 1);

            return new WhatsappResponse {ResponseMessage = $"We ontvingen je bericht, {userInfo.Name}"};
        }

        private async Task<WhatsappResponse> ProcessImages(UserInfo userInfo, WhatsappMessage message)
        {
            foreach (var mediaItem in message.MediaItems)
            {
                await _mediaPersister.PersistMediaFile(userInfo, WebUtility.UrlDecode(mediaItem.Url));
                _logger.LogEvent("New Image Received");
                _logger.LogMetric("Image Received", 1);
            }

            string responseMessage = message.MediaItems.Count == 1
                ? $"We stuurden je foto door, {userInfo.Name}"
                : $"We stuurden je {message.MediaItems.Count} foto's door, {userInfo.Name}";
            return new WhatsappResponse {ResponseMessage = responseMessage};
        }
    }
}