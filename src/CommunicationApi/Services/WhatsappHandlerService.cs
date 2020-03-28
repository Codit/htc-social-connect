using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace CommunicationApi.Services
{
    public class WhatsappHandlerService : IWhatsappHandlerService
    {
        private IUserMatcher _userMatcher;
        private IMediaServiceProvider _imageServiceProvider;
        private IMediaServiceProvider _messageServiceProvider;
        private ILogger<WhatsappHandlerService> _logger;
        private IMessageTranslater _messageTranslater;
        private IUserStore _userStore;

        public WhatsappHandlerService(IEnumerable< IMediaServiceProvider> mediaServiceProviders,
            IUserMatcher userMatcher, ILogger<WhatsappHandlerService> logger, IMessageTranslater messageTranslater,
            IUserStore userStore)
        {
            Guard.NotNull(userMatcher, nameof(userMatcher));
            Guard.NotNull(messageTranslater, nameof(messageTranslater));
            Guard.NotNull(mediaServiceProviders, nameof(mediaServiceProviders));
            Guard.NotNull(logger, nameof(logger));
            Guard.NotNull(userStore, nameof(userStore));
            _userMatcher = userMatcher;
            _imageServiceProvider = mediaServiceProviders.FirstOrDefault(msp => msp.SupportedType==MediaType.Image);
            _messageServiceProvider = mediaServiceProviders.FirstOrDefault(msp => msp.SupportedType==MediaType.Text);
            _logger = logger;
            _userStore = userStore;
            _messageTranslater = messageTranslater;
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
                    return await ProcessAuthenticatedMessage(userInfo, whatsappMessage);
                }

                // This is for users that are not yet linked to a tenant
                return await ProcessUnauthenticatedMessage(userInfo, whatsappMessage);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"An error happened while processing the incoming WhatsApp message with parameters {pars}");
                throw;
            }
        }

        private async Task<WhatsappResponse> ProcessAuthenticatedMessage(UserInfo userInfo, WhatsappMessage message)
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

        private async Task<WhatsappResponse> ProcessUnauthenticatedMessage(UserInfo userInfo, WhatsappMessage message)
        {
            string responseMessage;
            object[] pars = {userInfo.Name};

            string command = message.MessageContent;
            
            switch (userInfo.ConversationState)
            {
                case ConversationState.New:
                    //TODO : add user to store (phone number)
                    await _userStore.CreateUser(userInfo.PhoneNumber, null, ConversationState.AwaitingName);
                    responseMessage = "Welkom bij deze app, wat is uw naam, aub?";
                    break;
                case ConversationState.AwaitingName:
                    //TODO : update user to store (phone number + name)
                    userInfo.Name = command;
                    userInfo.ConversationState = ConversationState.AwaitingActivation;
                    await _userStore.UpdateUser(userInfo);
                    responseMessage =
                        "Welkom, {0}, als je een client wil connecteren, gelieve dan de activatiecode te sturen";
                    break;
                case ConversationState.AwaitingActivation:
                    //TODO : link user with existing tenant, if box is found
                    
                    userInfo.Name = command;
                    userInfo.ConversationState = ConversationState.Completed;
                    await _userStore.UpdateUser(userInfo);
                    responseMessage = "Het spijt ons, maar de activatie code is niet correct.  Kan u opnieuw proberen?";
                    responseMessage = "Dank je wel, de TV wordt opgezet om foto's en berichten te ontvangen";
                    break;
                case ConversationState.Completed:
                    //TODO : We should not be here : so add logging/warning
                    responseMessage =
                        "Normaal mag je nu gewoon foto's en berichten beginnen sturen en mag je dit niet meer krijgen";
                    break;
                default:
                    //TODO : We should not be here : so add logging/warning
                    responseMessage= "We konden uw bericht niet correct interpreteren.  Gelieve opnieuw te proberen";
                    break;
            }

            return new WhatsappResponse
            {
                ResponseMessage = (await _messageTranslater.Translate(userInfo.GetLanguage(), responseMessage, pars)),
                Accepted = true
            };
        }

        private async Task<WhatsappResponse> ProcessText(UserInfo userInfo, WhatsappMessage message)
        {
            string userMessage = $"Bericht van {userInfo.Name}({userInfo.PhoneNumber}): {message}";
            await _messageServiceProvider.PersistTextMessage(userInfo, new TextMessage
            {
                From = userInfo.Name,
                PhoneNumber = userInfo.PhoneNumber,
                Message = message.MessageContent,
                ExpirationTime = DateTimeOffset.UtcNow.AddDays(1)
            });

            _logger.LogEvent("New Message Received");
            _logger.LogMetric("Text Received", 1);

            return new WhatsappResponse
            {
                ResponseMessage = await _messageTranslater.Translate(userInfo.GetLanguage(),
                    "We ontvingen je bericht, {0}", userInfo.Name),
                Accepted = true
            };
        }

        private async Task<WhatsappResponse> ProcessImages(UserInfo userInfo, WhatsappMessage message)
        {
            foreach (var mediaItem in message.MediaItems)
            {
                await _imageServiceProvider.PersistMediaFile(userInfo, WebUtility.UrlDecode(mediaItem.Url));
                _logger.LogEvent("New Image Received");
                _logger.LogMetric("Image Received", 1);
            }

            string responseMessage = message.MediaItems.Count == 1
                ? await _messageTranslater.Translate(userInfo.GetLanguage(), "We stuurden je foto door, {0}",
                    userInfo.Name)
                : await _messageTranslater.Translate(userInfo.GetLanguage(), "We stuurden je {0} foto's door, {0}",
                    message.MediaItems.Count, userInfo.Name);
            return new WhatsappResponse
            {
                ResponseMessage = responseMessage,
                Accepted = true
            };
        }
    }
}