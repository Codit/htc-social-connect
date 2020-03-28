using System.Threading.Tasks;
using Bogus;
using CommunicationApi.Contracts.v1;
using CommunicationApi.Interfaces;
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
        private readonly Randomizer _boxCodeGenerator = new Randomizer();
        private readonly ILogger<BoxController> _logger;
        private readonly IBoxStore _boxStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxController"/> class.
        /// </summary>
        public BoxController(IBoxStore boxStore, ILogger<BoxController> logger)
        {
            Guard.NotNull(logger, nameof(logger));

            _logger = logger;
            _boxStore = boxStore;
        }

        /// <summary>
        ///     Register Box
        /// </summary>
        /// <remarks>Register a new box.</remarks>
        [HttpPost("new", Name = "Box_New")]
        [ProducesResponseType(typeof(ActivatedDevice), StatusCodes.Status201Created)]
        public async Task<IActionResult> New()
        {
            var activationCode = _boxCodeGenerator.Replace("????");
            var boxId = _boxCodeGenerator.Guid().ToString();

            var activatedDevice = new ActivatedDevice
            {
                ActivationCode = activationCode,
                BoxId = boxId
            };

            await _boxStore.Add(boxId, activatedDevice);

            return Created(Url.Action(nameof(GetStatus), new { boxId }), activatedDevice);
        }

        /// <summary>
        ///     Get Box Status
        /// </summary>
        /// <remarks>Get status for a registered box.</remarks>
        [HttpGet("status", Name = "Box_GetStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetStatus([FromQuery] string boxId)
        {
            if (string.IsNullOrWhiteSpace(boxId))
            {
                return BadRequest("No box is was specified");
            }

            var foundDevice = await _boxStore.Get(boxId);

            return foundDevice != null ? Ok(foundDevice) : (ActionResult)NotFound();
        }
    }
}