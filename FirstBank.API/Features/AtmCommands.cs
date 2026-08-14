using FirstBank.Core.Models;
using MediatR;
using System;

namespace FirstBank.API.Features
{
    public class AtmAuthCommand : IRequest<ApiResponse<object>>
    {
        public string CardNumber { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }
    public class AtmWithdrawCommand : IRequest<ApiResponse<object>>
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
