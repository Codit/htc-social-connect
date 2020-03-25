using System.Linq;
using System.Threading.Tasks;
using CommunicationHandlerApi.Interfaces;
using CommunicationHandlerApi.Models;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Twilio.TwiML;
using Twilio.TwiML.Messaging;

namespace CommunicationHandlerApi.Controllers
{
    //MediaContentType0=image%2Fjpeg&SmsMessageSid=MM4357bb89d143b4e79a71e98705a58b30&NumMedia=1&SmsSid=MM4357bb89d143b4e79a71e98705a58b30&SmsStatus=received&Body=&To=whatsapp%3A%2B14155238886&NumSegments=1&MessageSid=MM4357bb89d143b4e79a71e98705a58b30&AccountSid=AC93857f5d3c9313d91a32c1359c684f6e&From=whatsapp%3A%2B32474849993&MediaUrl0=https%3A%2F%2Fapi.twilio.com%2F2010-04-01%2FAccounts%2FAC93857f5d3c9313d91a32c1359c684f6e%2FMessages%2FMM4357bb89d143b4e79a71e98705a58b30%2FMedia%2FMEf2de932b5583a65738a0a3d62e501790&ApiVersion=2010-04-01
    /// <summary>
    /// API endpoint to check the health of the application.
    /// </summary>
    [ApiController]
    [Route("api/v1/whatsapp")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsappHandlerService _whatsappHandlerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhatsAppController"/> class.
        /// </summary>
        /// <param name="whatsappHandlerService">The service to handle the WhatsApp requests of the API application.</param>
        public WhatsAppController(IWhatsappHandlerService whatsappHandlerService)
        {
            Guard.NotNull(whatsappHandlerService, nameof(whatsappHandlerService));

            _whatsappHandlerService = whatsappHandlerService;
        }

        /// <summary>
        ///     Handle Request
        /// </summary>
        /// <remarks>Handles incoming Whatsapp requests.</remarks>
        [HttpPost(Name = "Whatsapp_Process")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Post([FromForm]IFormCollection collection)
        {
            var pars = collection.Keys
                .Select(key => new { Key = key, Value = collection[key] })
                .ToDictionary(p => p.Key, p => p.Value.ToString());
            var response = await _whatsappHandlerService.ProcessRequest(pars);

            var twiml = GenerateTwilioResponse(response);
            
            return new ContentResult
            {
                Content = twiml.ToString(),
                ContentType = "application/xml",
                StatusCode = 200
            };
        }

        private MessagingResponse GenerateTwilioResponse(WhatsappResponse response)
        {
            var twiml = new MessagingResponse();
            var twimlMessage = new Message();
            twimlMessage.Body(response.ResponseMessage);
            twiml.Append(twimlMessage);
            return twiml;
        }
    }
}