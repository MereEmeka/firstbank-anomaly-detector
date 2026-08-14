using MediatR;
using FirstBank.Core.Models;

namespace FirstBank.API.Features
{
    public class GetMyAccountDetailsQuery : IRequest<ApiResponse<AccountDashboardDto>>
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class AccountDashboardDto
    {
        public Guid AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public UserProfileDto User { get; set; } = new();
    }

    public class UserProfileDto
    {
        public string FullName { get; set; } = string.Empty;
    }
}