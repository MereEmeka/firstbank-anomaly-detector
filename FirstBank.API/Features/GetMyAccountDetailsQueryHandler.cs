using MediatR;
using Microsoft.EntityFrameworkCore;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Data;
using System.Threading;
using System.Threading.Tasks;

namespace FirstBank.API.Features
{
    public class GetMyAccountDetailsQueryHandler : IRequestHandler<GetMyAccountDetailsQuery, ApiResponse<AccountDashboardDto>>
    {
        private readonly FirstDBContext _context;

        public GetMyAccountDetailsQueryHandler(FirstDBContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<AccountDashboardDto>> Handle(GetMyAccountDetailsQuery request, CancellationToken cancellationToken)
        {
            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId.ToString() == request.UserId, cancellationToken);

            if (account == null)
            {
                return new ApiResponse<AccountDashboardDto>
                {
                    Success = false,
                    StatusCode = 404,
                    Message = "Account profile not found."
                };
            }

            return new ApiResponse<AccountDashboardDto>
            {
                Success = true,
                StatusCode = 200,
                Message = "Dashboard data retrieved successfully.",
                Data = new AccountDashboardDto
                {
                    AccountId = account.Id,
                    AccountNumber = account.AccountNumber,
                    Balance = account.Balance,
                    User = new UserProfileDto
                    {
                        FullName = $"{account.User.FirstName} {account.User.LastName}"
                    }
                }
            };
        }
    }
}