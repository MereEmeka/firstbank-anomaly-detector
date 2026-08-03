using FirstBank.Core.Models;
using MediatR;

namespace FirstBank.API.Features
{
    public class CreateAccountCommand : IRequest<ApiResponse<object>>
    {
        public Guid UserId { get; set; }
        public decimal InitialBalance { get; set; }
        public string Currency { get; set; } = "NGN";
    }
}
