using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using FirstBank.API.DTOs;
using FirstBank.API.Features;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FirstBank.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AnomalyController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AnomalyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Scan")]
        public async Task<IActionResult> ScanTransaction([FromBody] AnomalyScanRequest request)
        {
            var command = new AnalyzeTransactionCommand
            {
                AccountId = request.AccountId,
                TransactionAmount = request.TransactionAmount,
                Location = request.Location
            };

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // GET: api/anomaly/logs
        [HttpGet("logs")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSecurityLogs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize > 50) pageSize = 50;

            var query = new GetAnomalyLogsQuery(pageNumber, pageSize);

            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }
    }
}
