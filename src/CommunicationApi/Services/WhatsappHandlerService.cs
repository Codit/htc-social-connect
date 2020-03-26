using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CommunicationApi.Extensions;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using GuardNet;

namespace CommunicationApi.Services
{
    public class WhatsappHandlerService : IWhatsappHandlerService
    {
        private IUserMatcher _userMatcher;
        private IMediaPersister _mediaPersister;
        private IMessagePersister _messagePersister;

        public WhatsappHandlerService(IMessagePersister messagePersister, IMediaPersister mediaPersister, IUserMatcher userMatcher)
        {
            Guard.NotNull(userMatcher, nameof(userMatcher));
            Guard.NotNull(messagePersister, nameof(messagePersister));
            Guard.NotNull(mediaPersister, nameof(mediaPersister));
            _userMatcher = userMatcher;
            _mediaPersister = mediaPersister;
            _messagePersister = messagePersister;
        }

        public async Task<WhatsappResponse> ProcessRequest(Dictionary<string, string> pars)
        {
            var tenantInfo = await _userMatcher.Match(pars);
            var mediaCount = int.Parse((string) pars.GetParameter("NumMedia", "0"));
            if (mediaCount > 0)
            {
                return await ProcessImage(tenantInfo, pars, mediaCount);
            }
            else
            {
                return await ProcessText(tenantInfo, pars);
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
            return new WhatsappResponse{ ResponseMessage = $"We ontvingen je bericht, {userInfo.Name}"};
        }

        private async Task<WhatsappResponse> ProcessImage(UserInfo userInfo, Dictionary<string, string> pars, int mediaCount)
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
                }

                currentMediaId++;
            }

            string message = $"We stuurden je foto door, {userInfo.Name}";
            if (mediaCount > 1)
            {
                message =$"We stuurden je {mediaCount} foto's door, {userInfo.Name}";
            }
            return new WhatsappResponse{ ResponseMessage = message};
        }


    }
}