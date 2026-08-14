using MediatR;
using FirstBank.Core.Models;

namespace FirstBank.API.Features
{
    public class GetAccountBalanceQuery : IRequest<ApiResponse<object>>
    {
        public Guid AccountId { get; set; }
        public Guid UserId { get; set; }
    }
}