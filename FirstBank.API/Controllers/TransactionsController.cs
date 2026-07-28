using MediatR;
using FirstBank.API.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FirstBank.API.DTOs;
using FirstBank.Core.Models;
using FirstBank.API.Validators;
using FirstBank.DataAccess.Repositories;
using FirstBank.DataAccess.Data;
using Dapper;
using Microsoft.AspNetCore.RateLimiting; // Needed to see ITransactionRepository

namespace FirstBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly FirstDBContext _context;

        // Dependency Injection: The controller asks for the database connection
        public TransactionsController(ITransactionRepository repository, FirstDBContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

      [HttpPost]
      [EnableRateLimiting("TransactionPolicy")]
       public async Task<IActionResult> SubmitTransaction(
      [FromHeader(Name = "X-Idempotency-key")] string idempotencyKey,
      [FromBody] CreateTransactionRequest request)
        {
            // This Checks if header is completely missing
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "Missing X-Idempotency-key header."
                });
            }

            // Input Validation
            var validator = new CreateTransactionValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "Validation failed",
                    Data = validationResult.Errors.Select(e => e.ErrorMessage)
                });
            }

            // Package the data into the Command Envelope
            var command = new CreateTransactionCommand
            {
                SourceAccountId = Guid.Parse(request.SourceAccountId),
                DestinationAccountId = Guid.Parse(request.DestinationAccountId),
                Amount = request.Amount,
                Description = request.Description,
                IdempotencyKey = idempotencyKey
            };

            // Send to MediatR (This instantly triggers CreateTransactionCommandHandler)
            var response = await _mediator.Send(command);

            // Check if the Handler rejected the request (e.g., Duplicate Idempotency Key)
            if (response.StatusCode == 400)
            {
                return BadRequest(response);
            }

            // Return 201 Created
            return StatusCode(201, response);
        }

        [HttpGet("anomalies")]
        [Authorize(Roles = "Admin")] //Only Tokens with the "Admin" role gets in here.

        //The "1" and "10" are default values for pagination. If the user doesn't provide them, these will be used.
        public async Task<IActionResult> GetAnomalies([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetAnomalyLogsQuery(pageNumber, pageSize);

            //This fetches the real data from the database vault
            var logs = await _mediator.Send(query);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = 200,
                Message = $"You have successfully accessed the Admin-only Anomaly Logs vault." +
                $" Page: {pageNumber}, Size: {pageSize}",
                Data = logs
            });
        }


        /*
        [HttpGet("vulnerable")]
        [AllowAnonymous] //This endpoint is open to the public, no token required.
        public async Task<IActionResult> VulnerableSearch([FromQuery] string description)
        {
            //This demonstrates a vulnerable SQL query that is susceptible to SQL injection attacks.
            var sql = $"SELECT * FROM Transactions WHERE Description = '{description}'";

            var connection = _context.Database.GetDbConnection();
            var results = await connection.QueryAsync<Transaction>(sql);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = 200,
                Message = "Vulnerable search executed successfully.",
                Data = results
            });
        }
        */
         //https://localhost:7272/api/Transactions/secure-search?description='OR'1'='1

        [HttpGet("secure-search")]
        [Authorize]
        public async Task<IActionResult> SecureSearch([FromQuery] string description)
        {
            //The Fix: Using @Description as a safe parameter placeholder in the SQL query, and passing the actual value separately.
            var sql = "SELECT * FROM Transactions WHERE Description = @Description";

            var connection = _context.Database.GetDbConnection();

            //Dapper securely binds the variable as data, not executable code, preventing SQL injection attacks.
            var results = await connection.QueryAsync<Transaction>(sql, new { Description = description });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = 200,
                Message = "Secure Search executed successfully.",
                Data = results
            });
        }

        [HttpGet("balance/{accountId}")]
        public async Task<IActionResult> GetBalance(Guid accountId)
        {
            var query = new GetAccountBalanceQuery
            {
                AccountId = accountId
            };

            var response = await _mediator.Send(query);

            return Ok(response);
        }
    }
}