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
    [Route("api/[controller]")] // Translates to /api/accounts
    public class AccountsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AccountsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // NEW: Endpoint for the dynamic SPA Dashboard (GET /api/accounts/me)
        [HttpGet("me")]
        public async Task<IActionResult> GetMyAccount()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 401,
                    Message = "Invalid token claims. Please log in again."
                });
            }

            var query = new GetMyAccountDetailsQuery { UserId = userIdClaim };
            var result = await _mediator.Send(query);

            if (result == null || !result.Success)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 404,
                    Message = "Account not found for the current user."
                });
            }

            return Ok(result);
        }

        // POST: api/accounts
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
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

            var command = new CreateAccountCommand
            {
                UserId = currentUserId,
                InitialBalance = request.InitialBalance,
                Currency = request.Currency
            };

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // GET: api/accounts/{accountId}/balance
        [HttpGet("balance/{accountId}")]
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

            // 1. Extract the logged-in user's ID from their JWT token
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            // 2. Pass BOTH the AccountId and the UserId to the mediator
            var query = new GetAccountBalanceQuery
            {
                AccountId = validGuid,
                UserId = Guid.Parse(userIdClaim!) // This prevents User A from viewing User B's balance
            };

            var balanceResponse = await _mediator.Send(query);

            // 3. The mediator already returns a perfectly formatted ApiResponse, 
            // so we can just return it directly instead of wrapping it in a new one.
            return StatusCode(balanceResponse.StatusCode, balanceResponse);
        }

        // POST: api/accounts/{accountId}/deposit
        [HttpPost("{accountId}/deposit")]
        public async Task<IActionResult> MakeDeposit(string accountId, [FromBody] DepositRequest request)
        {
            if (!Guid.TryParse(accountId, out Guid validAccountId))
            {
                return BadRequest(new ApiResponse<object> { Success = false, StatusCode = 400, Message = "Invalid Account ID format." });
            }

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