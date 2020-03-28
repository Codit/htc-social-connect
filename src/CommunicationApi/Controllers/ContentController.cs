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

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentController"/> class.
        /// </summary>
        public ContentController(ILogger<ContentController> logger)
        {
            Guard.NotNull(logger, nameof(logger));

            _logger = logger;
        }

        /// <summary>
        ///     Get Messages
        /// </summary>
        /// <remarks>Get messages that were sent to the user.</remarks>
        [HttpGet("messages", Name = "Content_GetMessages")]
        public IActionResult GetMessages()
        {
            return Ok();
        }

        /// <summary>
        ///     Get Images
        /// </summary>
        /// <remarks>Get images that were sent to the user.</remarks>
        [HttpGet("images", Name = "Content_GetImages")]
        public IActionResult GetImages()
        {
            return Ok();
        }
    }
}