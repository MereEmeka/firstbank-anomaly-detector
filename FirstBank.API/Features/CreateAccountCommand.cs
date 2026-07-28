using FirstBank.Core.Models;
using MediatR;

namespace FirstBank.API.Features
{
    public class CreateAccountCommand : IRequest<ApiResponse<object>>
    {
        public string AccountNumber { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; }
        public string Currency { get; set; } = "NGN";
    }
}
