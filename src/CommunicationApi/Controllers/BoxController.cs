using System;
using System.Threading.Tasks;
using AutoMapper;
using Bogus;
using CommunicationApi.Contracts.v1;
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
    [Route("api/v1/box")]
    public class BoxController : ControllerBase
    {
        private readonly Randomizer _boxCodeGenerator = new Randomizer();
        private readonly ILogger<BoxController> _logger;
        private readonly IMapper _mapper;
        private readonly IBoxStore _boxStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxController"/> class.
        /// </summary>
        public BoxController(IBoxStore boxStore, IMapper mapper, ILogger<BoxController> logger)
        {
            Guard.NotNull(logger, nameof(logger));

            _mapper = mapper;
            _logger = logger;
            _boxStore = boxStore;
        }

        /// <summary>
        ///     Register Box
        /// </summary>
        /// <remarks>Register a new box.</remarks>
        [HttpPost("new", Name = "Box_New")]
        [ProducesResponseType(typeof(Contracts.v1.ActivatedDevice), StatusCodes.Status201Created)]
        public async Task<IActionResult> New()
        {
            var activationCode = "";
            Random random = new Random();
            for (int i = 0; i < 5; i++)
            {
                activationCode += GetRandomCharacter(random);
            }

            var boxId = _boxCodeGenerator.Guid().ToString();

            var activatedDevice = new Contracts.v1.ActivatedDevice
            {
                Status = Contracts.v1.BoxStatus.Registered,
                ActivationCode = activationCode,
                BoxId = boxId
            };

            var boxToPersist = _mapper.Map<Models.ActivatedDevice>(activatedDevice);
            await _boxStore.Add(boxId, boxToPersist);

            return Created(Url.Action(nameof(GetStatus), new {boxId}), activatedDevice);
        }

        private static char GetRandomCharacter(Random random)
        {
            const string chars = "ABDEFGHIJKMNOPRSTVWZ";
            var generatedChar = chars[random.Next(chars.Length)];
            return generatedChar; // was upper case in the chars list
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

            var persistedBox = await _boxStore.Get(boxId);
            if (persistedBox == null)
            {
                return NotFound();
            }

            var boxInfo = _mapper.Map<Contracts.v1.ActivatedDevice>(persistedBox);
            return Ok(boxInfo);
        }
    }
}