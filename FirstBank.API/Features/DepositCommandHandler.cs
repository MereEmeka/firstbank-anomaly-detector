using System.Threading;
using System.Threading.Tasks;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirstBank.API.Features
{
    public class DepositCommandHandler : IRequestHandler<DepositCommand, ApiResponse<object>>
    {
        private readonly FirstDBContext _context;
        private readonly ILogger<DepositCommandHandler> _logger;

        public DepositCommandHandler(FirstDBContext context, ILogger<DepositCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<object>> Handle(DepositCommand request, CancellationToken cancellationToken)
        {
            // 1. FIND THE ACCOUNT AND VERIFY OWNERSHIP
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

            if (account == null)
            {
                return new ApiResponse<object> { Success = false, StatusCode = 404, Message = "Account not found." };
            }

            // Security Check: Does the JWT match the account owner?
            if (account.UserId != request.UserId)
            {
                _logger.LogWarning("Unauthorized deposit attempt. UserId: {UserId} tried to access AccountId: {AccountId}", request.UserId, request.AccountId);
                return new ApiResponse<object> { Success = false, StatusCode = 403, Message = "You are not authorized to deposit into this account." };
            }

            // 2. PROCESS THE DEPOSIT
            account.Balance += request.Amount;

            // 3. SAVE TO DATABASE
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deposited {Amount} into AccountId: {AccountId}. New Balance: {Balance}",
                request.Amount, account.Id, account.Balance);

            return new ApiResponse<object>
            {
                Success = true,
                StatusCode = 200,
                Message = "Deposit successful.",
                Data = new
                {
                    accountId = account.Id,
                    accountNumber = account.AccountNumber,
                    newBalance = account.Balance,
                    currency = account.Currency
                }
            };
        }
    }
}