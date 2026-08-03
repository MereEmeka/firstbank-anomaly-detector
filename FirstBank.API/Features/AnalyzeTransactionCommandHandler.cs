using System;
using System.Collections.Generic;
using FirstBank.DataAccess.Data;
using MediatR;
using FirstBank.Core.Models;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace FirstBank.API.Features
{
    public class AnalyzeTransactionCommandHandler : IRequestHandler<AnalyzeTransactionCommand, ApiResponse<object>>
    {
        private readonly FirstDBContext _context;
        private readonly ILogger<AnalyzeTransactionCommandHandler> _logger;
    
    public AnalyzeTransactionCommandHandler(FirstDBContext context, ILogger<AnalyzeTransactionCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<object>> Handle(AnalyzeTransactionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Anomaly Scan for AccountId : {AccountId}", request.AccountId);

            //1. Verify if the Account Exists
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);
            if (account is null)
            {
                return new ApiResponse<object> { Success = false, StatusCode = 404, Message = "Target account not found." };
            }

            //2. The Rule-Based Detection Engine
            int riskScore = 0;
            var flagReasons = new List<string>();

            //Rule 1: Massive Transaction Volume
            if (request.TransactionAmount > 5_000_000)
            {
                riskScore += 50;
                flagReasons.Add("Transaction exceeds 5,000,000 NGN threshold");
            }
            else if (request.TransactionAmount > 1_000_000)
            {
                riskScore += 25;
                flagReasons.Add("High Value transaction detected");
            }

            // Rule 2: Disproportionate ratio (transacting 10x their current balance)
            if (account.Balance > 0 && (request.TransactionAmount / account.Balance) > 10m)
            {
                riskScore += 30;
                flagReasons.Add("Unusual transaction amount");
            }

            //Rule 3: Geographic Anomaly Simulation
            if (!request.Location.Contains("Nigeria"))
            {
                riskScore += 20;
                flagReasons.Add("Transaction originating outside primary service region");
            }

            //Cap the score at 100
            if (riskScore > 100) riskScore = 100;

            bool isFraudulent = riskScore >= 70;

            //3. Automatic Database Logging
            if (isFraudulent)
            {
                var fraudLog = new AnomalyLog
                {
                    TransactionId = Guid.NewGuid(),
                    FlagReason = string.Join(" | ", flagReasons)
                };

                _context.AnomalyLogs.Add(fraudLog);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Fraud prevented. Logged Anomaly for Account : {AccountId}. Score : {RiskScore}", account.Id, riskScore);
            }

            //4. Return the verdict/result
            return new ApiResponse<object>
            {
                Success = true,
                StatusCode = 200,
                Message = isFraudulent ? "CRITICAL ALERT: Transaction flagged as potentially fraudulent." : "Transaction Cleared.",
                Data = new
                {
                    AccountId = account.Id,
                    RiskScore = riskScore,
                    MaxScore = 100,
                    IsFlagged = isFraudulent,
                    Reasons = flagReasons
                }
            };
        }
    }
}
