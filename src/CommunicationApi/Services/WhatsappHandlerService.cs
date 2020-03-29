using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using GuardNet;
using Microsoft.Extensions.Logging;
using Twilio.Types;

namespace CommunicationApi.Services
{
    public class WhatsappHandlerService : IWhatsappHandlerService
    {
        private IMediaServiceProvider _imageServiceProvider;
        private IMediaServiceProvider _messageServiceProvider;
        private ILogger<WhatsappHandlerService> _logger;
        private IMessageTranslater _messageTranslater;
        private IUserStore _userStore;
        private IBoxStore _boxStore;

        public WhatsappHandlerService(IEnumerable<IMediaServiceProvider> mediaServiceProviders,
            ILogger<WhatsappHandlerService> logger, IMessageTranslater messageTranslater, IBoxStore boxStore,
            IUserStore userStore)
        {
            Guard.NotNull(messageTranslater, nameof(messageTranslater));
            Guard.NotNull(mediaServiceProviders, nameof(mediaServiceProviders));
            Guard.NotNull(logger, nameof(logger));
            Guard.NotNull(userStore, nameof(userStore));
            Guard.NotNull(boxStore, nameof(boxStore));
            _imageServiceProvider = mediaServiceProviders.FirstOrDefault(msp => msp.SupportedType == MediaType.Image);
            _messageServiceProvider = mediaServiceProviders.FirstOrDefault(msp => msp.SupportedType == MediaType.Text);
            _logger = logger;
            _boxStore = boxStore;
            _userStore = userStore;
            _messageTranslater = messageTranslater;
        }

        public async Task<WhatsappResponse> ProcessRequest(Dictionary<string, string> pars)
        {
            try
            {
                var whatsappMessage = new WhatsappMessage(pars);
                // Check in cache (or table) if phone Number is linked
                var userInfo = await _userStore.GetUserInfo(whatsappMessage.Sender);

                if (whatsappMessage.IsSystemMessage(out var userCommand))
                {
                    return await ProcessSystemMessage(userCommand, userInfo, whatsappMessage);
                }

                if (!string.IsNullOrEmpty(userInfo?.BoxInfo?.BoxId))
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

        private async Task<WhatsappResponse> ProcessSystemMessage(UserCommand userCommand, UserInfo userInfo,
            WhatsappMessage whatsappMessage)
        {
            string responseMessage;
            object[] pars = {userInfo.Name};
            bool accepted = false;
            switch (userCommand)
            {
                case UserCommand.LeaveBox:
                    userInfo.ConversationState = ConversationState.AwaitingActivation;
                    userInfo.BoxInfo = null;
                    pars = new object[] {userInfo.Name};
                    await _userStore.UpdateUser(userInfo);
                    _logger.LogWarning("The user {phoneNumber} got discoupled from the box {boxId}",
                        userInfo.PhoneNumber, userInfo.BoxInfo?.BoxId);
                    responseMessage = "We hebben u ontkoppeld van de box, {0}";
                    accepted = true;
                    break;
                case UserCommand.ViewBox:
                    _logger.LogWarning("The user {phoneNumber} asked a view link for the box {boxId}",
                        userInfo.PhoneNumber, userInfo.BoxInfo?.BoxId);
                    if (!string.IsNullOrEmpty(userInfo.BoxInfo?.BoxId))
                    {
                        pars = new object[] {$"https://coditfamilyview.azurewebsites.net?boxId={userInfo.BoxInfo.BoxId}"};
                        responseMessage = "U kan hier de box zien: {0}";
                        accepted = true;
                    }
                    else
                    {
                        pars = new object[] {userInfo.Name};
                        responseMessage = "U bent nog niet gekoppeld aan een box, {0}";
                    }
                    break;
                default:
                    responseMessage =
                        $"Het spijt ons, maar we hebben nog geen ondersteuning voor het commando {whatsappMessage.MessageContent}";
                    break;
            }

            return new WhatsappResponse
            {
                ResponseMessage = (await _messageTranslater.Translate(userInfo.GetLanguage(), responseMessage, pars)),
                Accepted = accepted
            };
        }

        private async Task<WhatsappResponse> ProcessAuthenticatedMessage(UserInfo userInfo, WhatsappMessage message)
        {
            // This is handling a user that is already linked to a tenant
            _logger.LogTrace(
                $"Processing message from user {userInfo.Name} and phone nr {userInfo.PhoneNumber} in tenant {userInfo.BoxInfo.BoxId}");
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

            string messageBody = message.MessageContent;
            string mediaUrl = null;
            switch (userInfo.ConversationState)
            {
                case ConversationState.New:
                    await _userStore.CreateUser(userInfo.PhoneNumber, null, ConversationState.AwaitingName);
                    responseMessage = "Welkom bij deze app, wat is uw naam, aub?";
                    break;
                case ConversationState.AwaitingName:
                    userInfo.Name = messageBody;
                    userInfo.ConversationState = ConversationState.AwaitingActivation;
                    pars = new object[] {userInfo.Name};
                    await _userStore.UpdateUser(userInfo);
                    responseMessage =
                        "Welkom, {0}, als je een client wil connecteren, gelieve dan de activatiecode te sturen.  Die kan je bekomen door de eerste letters te nemen van de icoontjes op het scherm van de box.  In het voorbeeld is het bijvoorbeeld HOME";
                    mediaUrl = "https://codithtc.blob.core.windows.net/public/media/example.png";
                    break;
                case ConversationState.AwaitingActivation:
                    responseMessage = await HandleBoxActivation(userInfo, messageBody);
                    break;
                case ConversationState.Completed:
                    _logger.LogWarning(
                        "User {phoneNumber} ended up in the unauthenticated conversation part, although the conversation state was completed",
                        userInfo.PhoneNumber);
                    responseMessage =
                        "Normaal mag je nu gewoon foto's en berichten beginnen sturen en mag je dit niet meer krijgen";
                    break;
                default:
                    _logger.LogWarning(
                        "User {phoneNumber} ended up in the unauthenticated conversation part, although the conversation state was completed",
                        userInfo.PhoneNumber);
                    responseMessage = "We konden uw bericht niet correct interpreteren.  Gelieve opnieuw te proberen";
                    break;
            }

            return new WhatsappResponse
            {
                ResponseMessage = (await _messageTranslater.Translate(userInfo.GetLanguage(), responseMessage, pars)),
                ImageUrl = mediaUrl,
                Accepted = true
            };
        }

        private async Task<string> HandleBoxActivation(UserInfo userInfo, string messageBody)
        {
            string responseMessage;
            var boxId = await _boxStore.Activate(messageBody);
            if (boxId == null)
            {
                _logger.LogWarning(
                    "User {phoneNumber} incorrectly wanted to activate a box with activation code {activationCode}",
                    userInfo.PhoneNumber, messageBody);
                responseMessage =
                    "Het spijt ons, maar de activatie code is niet correct.  Kan u opnieuw proberen?";
            }
            else
            {
                _logger.LogInformation(
                    "User {phoneNumber} successfully activate a box with activation code {activationCode} and box id {boxId}",
                    userInfo.PhoneNumber, messageBody, boxId);

                userInfo.ConversationState = ConversationState.Completed;
                if (userInfo.BoxInfo == null)
                {
                    userInfo.BoxInfo = new BoxInfo();
                }

                userInfo.BoxInfo.BoxId = boxId;
                await _userStore.UpdateUser(userInfo);
                responseMessage =
                    $"Dank je wel, de TV wordt opgezet om foto's en berichten te ontvangen.  Veel plezier met het sturen van foto's!";
            }

            return responseMessage;
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