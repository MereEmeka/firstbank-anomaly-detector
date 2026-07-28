using System;
using Microsoft.AspNetCore.Mvc;
using TheFirstBank.Core.Models;
using TheFirstBank.DataAccess.Repositories;

namespace TheFirstBank.API.Controllers
{
    [ApiController]
    [Route("api/[contoller]")] //This translates to /api/accounts
    public class  AccountsController : ControllerBase
    {
        private readonly ITransactionRepository _repository;
        public AccountsController(ITransactionRepository repository)
        {
            _repository = repository;
        }

        //The route expects an ID in the URL e.g. /api/accounts/1234/balance
        [HttpGet("{accountId}/balance")]
        public async Task<IActionResult> GetBalance(string accountId)
        {
            if (!Guid.TryParse(accountId, out Guid validGuid))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    StatusCodee = 400,
                    Message = "Invalid Account ID Format."
                });
            }
        }
    }
}
