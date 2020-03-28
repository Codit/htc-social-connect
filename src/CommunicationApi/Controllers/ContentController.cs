using System.Collections.Generic;
using System.Linq;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CommunicationApi.Controllers
{
    /// <summary>
    /// API endpoint to get content that was sent.
    /// </summary>
    [ApiController]
    [Route("api/v1/content")]
    public class ContentController : ControllerBase
    {
        private ILogger<ContentController> _logger;
        private IMediaServiceProvider _imageMediaServiceProvider;
        private IMediaServiceProvider _textMediaServiceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentController"/> class.
        /// </summary>
        public ContentController(ILogger<ContentController> logger, IEnumerable< IMediaServiceProvider> serviceProvider)
        {
            Guard.NotNull(logger, nameof(logger));
            Guard.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = logger;
            _imageMediaServiceProvider = serviceProvider.FirstOrDefault(msp => msp.SupportedType == MediaType.Image);
            _textMediaServiceProvider = serviceProvider.FirstOrDefault(msp => msp.SupportedType == MediaType.Text);
        }

        /// <summary>
        ///     Get Messages
        /// </summary>
        /// <remarks>Get messages that were sent to the user.</remarks>
        [HttpGet("messages", Name = "Content_GetMessages")]
        public IActionResult GetMessages()
        {
            //TODO joachim : use the _mediaServiceProvider to get messages
            
            return Ok();
        }

        /// <summary>
        ///     Get Images
        /// </summary>
        /// <remarks>Get images that were sent to the user.</remarks>
        [HttpGet("images", Name = "Content_GetImages")]
        public IActionResult GetImages()
        {
            //TODO joachim : use the _mediaServiceProvider to get images
            return Ok();
        }
    }
}