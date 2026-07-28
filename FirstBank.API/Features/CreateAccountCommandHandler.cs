using FirstBank.Core.Models;
using FirstBank.DataAccess.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirstBank.API.Features
{
    public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, ApiResponse<object>>
    {
        private readonly FirstDBContext _context;
        private readonly ILogger<CreateAccountCommandHandler> _logger;

        public CreateAccountCommandHandler(FirstDBContext context, ILogger<CreateAccountCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<object>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to create account: {AccountNumber}", request.AccountNumber);

            var exists = await _context.Accounts.AnyAsync(a => a.AccountNumber == request.AccountNumber, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("Account Creation failed. The account number: {AccountNumber} already exists.", request.AccountNumber);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Account Number already exists."
                };
            }

            var account = new Account
            {
                Id = Guid.NewGuid(),
                AccountNumber = request.AccountNumber,
                Balance = request.InitialBalance,
                Currency = request.Currency,
                CreatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created Account: {AccountId} for Account Number: {AccountNumber} with Starting Balance: {Balance}",
                account.Id, account.AccountNumber, account.Balance);

            return new ApiResponse<object>
            {
                Success = true,
                StatusCode = 201,
                Message = "Account Created Successfully.",
                Data = new
                {
                    account.Id,
                    account.AccountNumber,
                    account.Balance,
                    account.Currency
                }
            };
        }
    }
}