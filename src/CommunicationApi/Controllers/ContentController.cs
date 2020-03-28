using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using GuardNet;
using Microsoft.AspNetCore.Http;
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
        public ContentController(ILogger<ContentController> logger, IEnumerable< IMediaServiceProvider> serviceProviders)
        {
            Guard.NotNull(logger, nameof(logger));
            Guard.NotNull(serviceProviders, nameof(serviceProviders));
            _logger = logger;
            _imageMediaServiceProvider = serviceProviders.FirstOrDefault(msp => msp.SupportedType == MediaType.Image);
            _textMediaServiceProvider = serviceProviders.FirstOrDefault(msp => msp.SupportedType == MediaType.Text);
        }

        /// <summary>
        ///     Get Messages
        /// </summary>
        /// <remarks>Get messages that were sent to the user.</remarks>
        [HttpGet("messages", Name = "Content_GetMessages")]
        [ProducesResponseType(typeof(IEnumerable<MediaItem>),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMessages([FromQuery] string boxId)
        {
            _logger.LogInformation("Getting message for box {BoxId}", boxId);

            var messages = await _textMediaServiceProvider.GetItems(boxId);
            _logger.LogMetric("Messages returned", messages.Count());

            return Ok(messages);
        }

        /// <summary>
        ///     Get Images
        /// </summary>
        /// <remarks>Get images that were sent to the user.</remarks>
        [HttpGet("images", Name = "Content_GetImages")]
        [ProducesResponseType(typeof(IEnumerable<MediaItem>),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetImages([FromQuery] string boxId)
        {
            _logger.LogEvent($"Getting images for box {boxId}");
            var images = await _imageMediaServiceProvider.GetItems(boxId);
            _logger.LogMetric("Images returned", images.Count());
            return Ok(images);
        }
    }
}
