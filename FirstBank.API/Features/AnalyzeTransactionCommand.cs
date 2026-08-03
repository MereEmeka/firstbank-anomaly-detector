using FirstBank.Core.Models;
using MediatR;

namespace FirstBank.API.Features
{
    public class AnalyzeTransactionCommand : IRequest<ApiResponse<object>>
    {
        public Guid AccountId { get; set; }
        public decimal TransactionAmount { get; set; }
        public string Location { get; set; } = string.Empty;
    }
}
