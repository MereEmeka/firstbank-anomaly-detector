using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using FirstBank.Core.Models;
using FirstBank.API.DTOs;
using FirstBank.API.Features;

namespace FirstBank.API.Controllers
{
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
            var command = new CreateAccountCommand
            {
                AccountNumber = request.AccountNumber,
                InitialBalance = request.InitialBalance,
                Currency = request.Currency
            };

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
    }
}