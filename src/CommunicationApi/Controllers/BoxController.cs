using System;
using CommunicationApi.Contracts.v1;
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
    [Route("api/v1/box")]
    public class BoxController : ControllerBase
    {
        private ILogger<BoxController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxController"/> class.
        /// </summary>
        public BoxController(ILogger<BoxController> logger)
        {
            Guard.NotNull(logger, nameof(logger));

            _logger = logger;
        }

        /// <summary>
        ///     Register Box
        /// </summary>
        /// <remarks>Register a new box.</remarks>
        [HttpPost("new", Name = "Box_New")]
        [ProducesResponseType(typeof(ActivatedDevice), StatusCodes.Status201Created)]
        public IActionResult New()
        {
            var activatedDevice = new ActivatedDevice
            {
                ActivationCode = Guid.NewGuid().ToString(),
                BoxId = Guid.NewGuid().ToString()
            };

            return Created(Url.Action(nameof(GetStatus), new { boxId = activatedDevice.BoxId }), activatedDevice);
        }

        /// <summary>
        ///     Get Box Status
        /// </summary>
        /// <remarks>Get status for a registered box.</remarks>
        [HttpGet("status", Name = "Box_GetStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetStatus([FromQuery] string boxId)
        {
            if (string.IsNullOrWhiteSpace(boxId))
            {
                return BadRequest("No box is was specified");
            }

            return NotFound();
        }
    }
}