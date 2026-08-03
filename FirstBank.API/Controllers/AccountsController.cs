using FirstBank.API.DTOs;
using FirstBank.API.Features;
using FirstBank.Core.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FirstBank.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")] // This translates to /api/accounts
    public class AccountsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AccountsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST: api/accounts
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            // 1. EXTRACT THE IDENTITY: Read the UserId directly from the encrypted JWT
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 401,
                    Message = "Invalid token claims. Please log in again."
                });
            }

            var currentUserId = Guid.Parse(userIdClaim);

            // 2. PACKAGE THE COMMAND: Pass the UserId instead of an AccountNumber
            var command = new CreateAccountCommand
            {
                UserId = currentUserId, // Securely pulled from the token
                InitialBalance = request.InitialBalance,
                Currency = request.Currency
            };

            // 3. SEND TO HANDLER: Let MediatR do the database work
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // GET: api/accounts/{accountId}/balance
        [HttpGet("{accountId}/balance")]
        public async Task<IActionResult> GetBalance(string accountId)
        {
            if (!Guid.TryParse(accountId, out Guid validGuid))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "Invalid Account ID Format."
                });
            }

            // This fetches the balance using MediatR query pipeline
            var balance = await _mediator.Send(new GetAccountBalanceQuery { AccountId = validGuid });

            if (balance is null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "Account not found."
                });
            }

            // This returns the Success envelope
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = 200,
                Message = "Balance retrieved successfully.",
                Data = new { balance = balance, currency = "NGN" }
            });
        }

        // POST: api/accounts/{accountId}/deposit
        [HttpPost("{accountId}/deposit")]
        public async Task<IActionResult> MakeDeposit(string accountId, [FromBody] DepositRequest request)
        {
            // Validate the GUID from the URL
            if (!Guid.TryParse(accountId, out Guid validAccountId))
            {
                return BadRequest(new ApiResponse<object> { Success = false, StatusCode = 400, Message = "Invalid Account ID format." });
            }

            // Extract the Identity securely
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, StatusCode = 401, Message = "Invalid token." });
            }

            var command = new DepositCommand
            {
                AccountId = validAccountId,
                UserId = Guid.Parse(userIdClaim),
                Amount = request.Amount
            };

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}