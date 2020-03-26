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
                var userInfo = await _userMatcher.Match(pars);
                _logger.LogTrace(
                    $"Processing message from user {userInfo.Name} and phone nr {userInfo.PhoneNumber} in tenant {userInfo.TenantInfo.Name}");
                var mediaCount = int.Parse((string) pars.GetParameter("NumMedia", "0"));
                if (mediaCount > 0)
                {
                    return await ProcessImage(userInfo, pars, mediaCount);
                }
                else
                {
                    return await ProcessText(userInfo, pars);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"An error happened while processing the incoming WhatsApp message with parameters {pars}");
                throw;
            }
        }

        private async Task<WhatsappResponse> ProcessText(UserInfo userInfo, Dictionary<string, string> pars)
        {
            string message = pars.GetParameter("Body");
            string userMessage = $"Bericht van {userInfo.Name}({userInfo.PhoneNumber}): {message}";
            await _messagePersister.PersistMessage(new TextMessage
            {
                From = userInfo.Name,
                PhoneNumber = userInfo.PhoneNumber,
                Message = message,
                ExpirationTime = DateTimeOffset.UtcNow.AddDays(1)
            }, userInfo);

            _logger.LogEvent("New Message Received");
            _logger.LogMetric("Image Received", 1);

            return new WhatsappResponse {ResponseMessage = $"We ontvingen je bericht, {userInfo.Name}"};
        }

        private async Task<WhatsappResponse> ProcessImage(UserInfo userInfo, Dictionary<string, string> pars,
            int mediaCount)
        {
            bool mediaFound = true;
            int currentMediaId = 0;
            while (mediaFound)
            {
                var mediaUrl = pars.GetParameter($"MediaUrl{currentMediaId}");
                mediaFound = !string.IsNullOrEmpty(mediaUrl);
                if (mediaFound)
                {
                    await _mediaPersister.PersistMediaFile(userInfo, WebUtility.UrlDecode(mediaUrl));
                    _logger.LogEvent("New Image Received");
                    _logger.LogMetric("Image Received", 1);
                }

                currentMediaId++;
            }

            string message = $"We stuurden je foto door, {userInfo.Name}";
            if (mediaCount > 1)
            {
                message = $"We stuurden je {mediaCount} foto's door, {userInfo.Name}";
            }
            
            return new WhatsappResponse {ResponseMessage = message};
        }
    }
}