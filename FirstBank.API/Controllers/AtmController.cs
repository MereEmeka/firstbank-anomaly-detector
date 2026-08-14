using FirstBank.API.Features;
using FirstBank.Core.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FirstBank.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AtmController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AtmController(IMediator mediator) => _mediator = mediator;

        [HttpPost("insert-card")]
        [AllowAnonymous]
        public async Task<IActionResult> Auth([FromBody] AtmAuthCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [Authorize]
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] AtmWithdrawCommand command)
        {
            // Security: We NEVER trust the client with the AccountId. 
            // We securely extract it from the ATM JWT Token generated during the PIN check.
            var accountIdClaim = User.Claims.FirstOrDefault(c => c.Type == "AccountId")?.Value;
            if (accountIdClaim is null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, StatusCode = 401, Message = "Invalid ATM Session" });
            }

            command.AccountId = Guid.Parse(accountIdClaim);
            var result = await _mediator.Send(command);

            return StatusCode(result.StatusCode, result);
        }
    }
}

