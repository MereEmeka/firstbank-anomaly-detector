using System;
using MediatR;
using FirstBank.Core.Models;

namespace FirstBank.API.Features
{
    public class DepositCommand : IRequest<ApiResponse<object>>
    {
        public Guid AccountId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
    }
}
