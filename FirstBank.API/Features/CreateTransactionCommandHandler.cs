using MediatR;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Data;
using FirstBank.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirstBank.API.Features
{
    public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, ApiResponse<object>>
    {
        private readonly FirstDBContext _context;
        private readonly ITransactionRepository _repository;
        private readonly ILogger<CreateTransactionCommandHandler> _logger;

        // MediatR automatically injects your database and repository here!
        public CreateTransactionCommandHandler(
            FirstDBContext context,
            ITransactionRepository repository,
            ILogger<CreateTransactionCommandHandler> logger)
        {
            _context = context;
            _repository = repository;
            _logger = logger;
        }

        public async Task<ApiResponse<object>> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing transaction with Idempotency Key: {IdempotencyKey} for Amount: {Amount} from Source: {SourceAccountId}",
                request.IdempotencyKey, request.Amount, request.SourceAccountId);

            // 1. CHECK THE SQL DATABASE FOR DUPLICATES
            bool keyExists = await _context.IdempotencyRecords.AnyAsync(i => i.Key == request.IdempotencyKey, cancellationToken);

            if (keyExists)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "Duplicate Request. This transaction has already been processed."
                };
            }

            // 2. BUSINESS LOGIC
            bool isAnomalyFlagged = request.Amount > 500000;

            if (isAnomalyFlagged)
            {
                _logger.LogWarning("Anomaly detected for Transaction with Idempotency Key: {IdempotencyKey} - Amount: {Amount} exceeds threshold",
                    request.IdempotencyKey, request.Amount);
            }

            // 3. MAP TO DOMAIN MODEL (AND ADD YOUR NEW STATUS TWEAK!)
            var transactionModel = new Transaction
            {
                Id = Guid.NewGuid(),
                SourceAccountId = request.SourceAccountId,
                DestinationAccountId = request.DestinationAccountId,
                Amount = request.Amount,
                Description = request.Description ?? "",
                IsAnomalyFlagged = isAnomalyFlagged,
                CreatedAt = DateTime.UtcNow,
                Status = "Completed"
            };

            // 4. SAVE TO DATABASE
            string savedTransactionId = await _repository.SubmitTransactionAsync(transactionModel, isAnomalyFlagged, request.IdempotencyKey ?? "");

            _logger.LogInformation("Transaction {TransactionId} submitted successfully for Amount {Amount} from Source {SourceAccountId}",
                savedTransactionId, request.Amount, request.SourceAccountId);


            // 5. RETURN SUCCESS
            return new ApiResponse<object>
            {
                Success = true,
                StatusCode = 201,
                Message = "Transaction submitted successfully.",
                Data = new { transactionId = savedTransactionId }
            };
        }
    }
}