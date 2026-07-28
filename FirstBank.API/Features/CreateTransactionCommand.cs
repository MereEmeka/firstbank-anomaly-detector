using FirstBank.Core.Models;
using MediatR;

namespace FirstBank.API.Features
{
    public class CreateTransactionCommand : IRequest<ApiResponse<object>>
    {
        public Guid SourceAccountId { get; set; }
        public Guid DestinationAccountId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }

        //This is the header, the '?' tells C# it's okay if it is null
        public string? IdempotencyKey { get; set; }
    }
}
