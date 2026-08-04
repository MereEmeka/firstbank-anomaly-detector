using System;
using FirstBank.Core.Constants;
using System.Collections.Generic;
using System.Security.Cryptography.Pkcs;
using System.Threading;
using System.Threading.Tasks;
using FirstBank.API.Services;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Data;
using FirstBank.DataAccess.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq; // Make sure Linq is included for FirstOrDefault

namespace FirstBank.API.Features
{
    public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, ApiResponse<object>>
    {
        private readonly FirstDBContext _context;
        private readonly ITransactionRepository _repository;
        private readonly ILogger<CreateTransactionCommandHandler> _logger;
        private readonly IEmailService _emailService;

        public CreateTransactionCommandHandler(
            FirstDBContext context,
            ITransactionRepository repository,
            ILogger<CreateTransactionCommandHandler> logger,
            IEmailService emailService)
        {
            _context = context;
            _repository = repository;
            _logger = logger;
            _emailService = emailService;
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

          
            // Fetch BOTH accounts and their User Profiles at the same time
         
            var accounts = await _context.Accounts
                .Include(a => a.User) // This joins the Users table so we have their names/emails
                .Where(a => a.Id == request.SourceAccountId || a.Id == request.DestinationAccountId)
                .ToListAsync(cancellationToken);

            var sourceAccount = accounts.FirstOrDefault(a => a.Id == request.SourceAccountId);
            var destAccount = accounts.FirstOrDefault(a => a.Id == request.DestinationAccountId);

            if (sourceAccount == null)
            {
                return new ApiResponse<object> { Success = false, StatusCode = 404, Message = "Source account not found." };
            }

            if (destAccount == null)
            {
                return new ApiResponse<object> { Success = false, StatusCode = 404, Message = "Destination account not found." };
            }

            if (sourceAccount.Balance < request.Amount)
            {
                return new ApiResponse<object> { Success = false, StatusCode = 400, Message = "Insufficient funds for this transfer." };
            }
         

            // 3. THE 100-POINT ANOMALY DETECTION ENGINE
            int riskScore = 0;
            var flagReasons = new List<string>();

            // Rule 1: Massive Transfer Volume (Fixed string interpolation $)
            if (request.Amount > SecurityThresholds.CriticalTransferLimit)
            {
                riskScore += SecurityThresholds.ScoreCriticalVolume;
                flagReasons.Add($"Transfer exceeds {SecurityThresholds.CriticalTransferLimit:N2} NGN threshold.");
            }
            else if (request.Amount > SecurityThresholds.HighValueTransferLimit)
            {
                riskScore += SecurityThresholds.ScoreHighVolume;
                flagReasons.Add("High-value transfer detected.");
            }

            // Rule 2: Account Drain (Fixed string interpolation $)
            if (sourceAccount.Balance > 0 && (request.Amount / sourceAccount.Balance) > SecurityThresholds.MaxBalanceDepletionRatio)
            {
                riskScore += SecurityThresholds.ScoreAccountDrain;
                flagReasons.Add($"Transfer depletes more than {SecurityThresholds.MaxBalanceDepletionRatio * 100}% of the balance.");
            }

            // Cap the score and evaluate
            if (riskScore > SecurityThresholds.MaximumRiskScore) riskScore = SecurityThresholds.MaximumRiskScore;
            bool isFraudulent = riskScore >= SecurityThresholds.FraudTriggerScore;

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
                var fraudLog = new AnomalyLog
                {
                    TransactionId = transactionModel.Id,
                    FlagReason = string.Join(" | ", flagReasons)
                };

                _context.AnomalyLogs.Add(fraudLog);
                await _context.SaveChangesAsync(cancellationToken);

                //Fire off the Email Alert
                _ = _emailService.SendFraudAlertAsync(
                    transactionModel.Id.ToString(),
                    request.SourceAccountId.ToString(),
                    request.Amount,
                    fraudLog.FlagReason);

                _logger.LogWarning("Transfer blocked by Anomaly Engine! Logged anomaly for Transaction: {TransactionId}. Score: {RiskScore}",
                    transactionModel.Id, riskScore);

                return new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 403,
                    Message = "CRITICAL: Transfer blocked by Anomaly Detector.",
                    Data = new { RiskScore = riskScore, Reasons = flagReasons }
                };
            }

            // 6. EXECUTE VALID TRANSFER VIA DAPPER AND SEND RECEIPT
            try
            {
                string savedTransactionId = await _repository.SubmitTransactionAsync(transactionModel, isFraudulent, request.IdempotencyKey ?? "");

                _logger.LogInformation("Transaction {TransactionId} submitted successfully for Amount {Amount} from Source {SourceAccountId}",
                    savedTransactionId, request.Amount, request.SourceAccountId);

           
                // Send emails to BOTH parties with correct formatting

                // Send Debit Alert to Source
                if (sourceAccount?.User != null)
                {
                    string debitBody = $@"
                        <h2>FirstBank Debit Alert</h2>
                        <p>Dear {sourceAccount.User.FirstName},</p>
                        <p>Your transfer was completed successfully.</p>
                        <hr />
                        <p><strong>Transaction ID:</strong> {savedTransactionId}</p>
                        <p><strong>Amount Debited:</strong> {request.Amount:N2} NGN</p>
                        <p><strong>New Balance:</strong> {(sourceAccount.Balance - request.Amount):N2} NGN</p>
                        <p><strong>Description:</strong> {request.Description ?? "N/A"}</p>
                        <p><strong>Date/Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                        <hr />
                        <p>Thank you for banking with FirstBank.</p>";

                    _ = _emailService.SendEmailAsync(
                        sourceAccount.User.Email,
                        "FirstBank: Debit Alert",
                        debitBody);
                }

                // Send Credit Alert to Destination
                if (destAccount?.User != null)
                {
                    string creditBody = $@"
                        <h2>FirstBank Credit Alert</h2>
                        <p>Dear {destAccount.User.FirstName},</p>
                        <p>Your account has been credited.</p>
                        <hr />
                        <p><strong>Transaction ID:</strong> {savedTransactionId}</p>
                        <p><strong>Amount Credited:</strong> {request.Amount:N2} NGN</p>
                        <p><strong>New Balance:</strong> {(destAccount.Balance + request.Amount):N2} NGN</p>
                        <p><strong>Description:</strong> {request.Description ?? "N/A"}</p>
                        <p><strong>Date/Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                        <hr />
                        <p>Thank you for banking with FirstBank.</p>";

                    _ = _emailService.SendEmailAsync(
                        destAccount.User.Email,
                        "FirstBank: Credit Alert",
                        creditBody);
                }

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
                return new ApiResponse<object> { Success = false, StatusCode = 400, Message = ex.Message };
            }
        }
    }
}