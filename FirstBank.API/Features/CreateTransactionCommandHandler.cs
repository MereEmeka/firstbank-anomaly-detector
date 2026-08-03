using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Data;
using FirstBank.DataAccess.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirstBank.API.Features
{
    public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, ApiResponse<object>>
    {
        private readonly FirstDBContext _context;
        private readonly ITransactionRepository _repository;
        private readonly ILogger<CreateTransactionCommandHandler> _logger;

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

            // 1. IDEMPOTENCY CHECK: Prevent double-processing
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

            // 2. BASIC VALIDATION: Check if source account exists and has enough funds
            var sourceAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.SourceAccountId, cancellationToken);

            if (sourceAccount == null)
            {
                return new ApiResponse<object> { Success = false, StatusCode = 404, Message = "Source account not found." };
            }

            if (sourceAccount.Balance < request.Amount)
            {
                return new ApiResponse<object> { Success = false, StatusCode = 400, Message = "Insufficient funds for this transfer." };
            }

            // 3. THE 100-POINT ANOMALY DETECTION ENGINE
            int riskScore = 0;
            var flagReasons = new List<string>();

            // Rule 1: Massive Transfer Volume
            if (request.Amount > 5_000_000)
            {
                riskScore += 50;
                flagReasons.Add("Transfer exceeds 5,000,000 NGN threshold.");
            }
            else if (request.Amount > 1_000_000)
            {
                riskScore += 25;
                flagReasons.Add("High-value transfer detected.");
            }

            // Rule 2: Account Drain (Attempting to transfer more than 90% of their total balance at once)
            if (sourceAccount.Balance > 0 && (request.Amount / sourceAccount.Balance) > 0.9m)
            {
                riskScore += 30;
                flagReasons.Add("Transfer depletes more than 90% of the source account balance.");
            }

            // Cap the score and evaluate
            if (riskScore > 100) riskScore = 100;
            bool isFraudulent = riskScore >= 70;

            // 4. MAP TO DOMAIN MODEL
            var transactionModel = new Transaction
            {
                Id = Guid.NewGuid(),
                SourceAccountId = request.SourceAccountId,
                DestinationAccountId = request.DestinationAccountId,
                Amount = request.Amount,
                Description = request.Description ?? "",
                IsAnomalyFlagged = isFraudulent,
                CreatedAt = DateTime.UtcNow,
                Status = isFraudulent ? "Blocked" : "Completed"
            };

            // 5. BLOCK AND LOG IF FRAUD DETECTED
            if (isFraudulent)
            {
                // Write directly to your new table
                var fraudLog = new AnomalyLog
                {
                    TransactionId = transactionModel.Id,
                    FlagReason = string.Join(" | ", flagReasons)
                };

                _context.AnomalyLogs.Add(fraudLog);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Transfer blocked by Anomaly Engine! Logged anomaly for Transaction: {TransactionId}. Score: {RiskScore}",
                    transactionModel.Id, riskScore);

                return new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 403, // 403 Forbidden because it's a security block
                    Message = "CRITICAL: Transfer blocked by Anomaly Detector.",
                    Data = new { RiskScore = riskScore, Reasons = flagReasons }
                };
            }

            // 6. EXECUTE VALID TRANSFER VIA DAPPER
            try
            {
                string savedTransactionId = await _repository.SubmitTransactionAsync(transactionModel, isFraudulent, request.IdempotencyKey ?? "");

                _logger.LogInformation("Transaction {TransactionId} submitted successfully for Amount {Amount} from Source {SourceAccountId}",
                    savedTransactionId, request.Amount, request.SourceAccountId);

                return new ApiResponse<object>
                {
                    Success = true,
                    StatusCode = 201,
                    Message = "Transaction submitted successfully.",
                    Data = new { transactionId = savedTransactionId }
                };
            }
            catch (InvalidOperationException ex)
            {
                // Catches custom SQL Server constraints from your stored procedure
                return new ApiResponse<object> { Success = false, StatusCode = 400, Message = ex.Message };
            }
        }
    }
}