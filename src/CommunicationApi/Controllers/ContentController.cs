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
        [ProducesResponseType(typeof(IEnumerable<MediaItem>),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMessages([FromQuery] string boxId)
        {
            var messages = await _textMediaServiceProvider.GetItems(boxId);
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
            var messages = await _imageMediaServiceProvider.GetItems(boxId);
            return Ok(messages);
        }
    }
}