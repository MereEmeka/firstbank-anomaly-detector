using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Repositories;
using Microsoft.Extensions.Logging;

namespace FirstBank.API.Features
{
    public class GetAnomalyLogsQueryHandler : IRequestHandler<GetAnomalyLogsQuery, ApiResponse<object>>
    {
        private readonly ITransactionRepository _repository;
        private readonly ILogger<GetAnomalyLogsQueryHandler> _logger;

        public GetAnomalyLogsQueryHandler(ITransactionRepository repository, ILogger<GetAnomalyLogsQueryHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        public async Task <ApiResponse<object>> Handle(GetAnomalyLogsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching anomaly logs for PageNumber: {PageNumber}, PageSize: {PageSize}", request.PageNumber, request.PageSize);

            // Calls the repository to get the paginated data
            var logs = await _repository.GetAnomalyLogsAsync(request.PageNumber, request.PageSize);

            return new ApiResponse<object>
            {
                Success = true,
                StatusCode = 200,
                Message = "Anomaly logs retrieved successfully",
                Data = logs
            };   

        }
    }
}
