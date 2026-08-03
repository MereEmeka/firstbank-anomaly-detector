using System;
using System.Threading;
using System.Threading.Tasks;
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
            // 1. FETCH THE ACTUAL USER FROM THE DATABASE
            // We use the ID from the JWT to look up their full profile
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (currentUser == null)
            {
                return new ApiResponse<object> { Success = false, StatusCode = 404, Message = "User profile not found." };
            }

            // 2. GENERATE UNIQUE ACCOUNT NUMBER
            var random = new Random();
            string generatedAccountNumber;
            bool isUnique;
            do
            {
                generatedAccountNumber = random.Next(1000000000, 2000000000).ToString();
                isUnique = !await _context.Accounts.AnyAsync(a => a.AccountNumber == generatedAccountNumber, cancellationToken);
            }
            while (!isUnique);

            // 3. BUILD THE ACCOUNT ENTITY
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                AccountNumber = generatedAccountNumber,
                Balance = request.InitialBalance,
                Currency = request.Currency,
                CreatedAt = DateTime.UtcNow
            };

            // 4. SAVE TO DATABASE
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created Account: {AccountNumber} for {FirstName} {LastName}",
                account.AccountNumber, currentUser.FirstName, currentUser.LastName);

            // 5. RETURN A BRILLIANT SUCCESS ENVELOPE
            // Notice how we include their actual name in the response data!
            return new ApiResponse<object>
            {
                Success = true,
                StatusCode = 201,
                Message = "Account Created Successfully.",
                Data = new
                {
                    accountId = account.Id,
                    accountOwner = $"{currentUser.FirstName} {currentUser.LastName}", 
                    accountNumber = account.AccountNumber,
                    balance = account.Balance,
                    currency = account.Currency
                }
            };
        }
    }
}