using FirstBank.API.Features;
using FirstBank.Core.Constants;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FirstBank.API.Features
{
    public class AtmWithdrawCommandHandler : IRequestHandler<AtmWithdrawCommand, ApiResponse<object>>
    {
        private readonly IMediator _mediator;
        private readonly FirstDBContext _context;

        public AtmWithdrawCommandHandler(IMediator mediator, FirstDBContext context)
        {
            _mediator = mediator;
            _context = context;
        }

        public async Task<ApiResponse<object>> Handle(AtmWithdrawCommand request, CancellationToken cancellationToken)
        {
            // 1. Hardware constraint checks
            if (request.Amount < 500) return new ApiResponse<object> { Success = false, StatusCode = 400, Message = "Minimum withdrawal is 500 NGN." };
            if (request.Amount > 150_000) return new ApiResponse<object> { Success = false, StatusCode = 400, Message = "Maximum per transaction is 150,000 NGN." };
            if (request.Amount > AtmVault.CurrentPhysicalReserve) return new ApiResponse<object> { Success = false, StatusCode = 503, Message = "ATM out of cash. Try a smaller amount." };

            // 2. Cross-Boundary Call: Ask Core Banking for the balance using AccountId
            var balanceResponse = await _mediator.Send(new GetAccountBalanceQuery { AccountId = request.AccountId }, cancellationToken);
            if (!balanceResponse.Success) return balanceResponse;

            // Extract the balance from the dynamic Data object
            decimal currentBalance = (decimal)((dynamic)balanceResponse.Data!).CurrentBalance;
            if (currentBalance < request.Amount) return new ApiResponse<object> { Success = false, StatusCode = 400, Message = "Insufficient Funds." };

         
            // Replace Step 3 (Cross-Boundary Call) with this direct debit:
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

            // Fix: Check if the account is null before touching the balance
            if (account == null)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 404,
                    Message = "Account not found."
                };
            }

            account.Balance -= request.Amount;
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Dispense physical cash
            AtmVault.CurrentPhysicalReserve -= request.Amount;

            return new ApiResponse<object>
            {
                Success = true,
                StatusCode = 200,
                Message = "Please take your cash.",
                Data = new { Dispensed = request.Amount, RemainingBalance = currentBalance - request.Amount, AtmReserve = AtmVault.CurrentPhysicalReserve }
            };
        }
    }
}