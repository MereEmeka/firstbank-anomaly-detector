using MediatR;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace FirstBank.API.Features
{
    public class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, ApiResponse<object>>
    {
        private readonly ITransactionRepository _repository;

        public GetAccountBalanceQueryHandler(ITransactionRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<object>> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
        {
            var balance = await _repository.GetAccountBalanceAsync(request.AccountId);

            if (balance == null)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 404,
                    Message = "Account not found"
                };
            }
               
            return new ApiResponse<object>
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "Account Balance Received successfully through Stored Procedure.",
                    Data = new
                    {
                        AccountId = request.AccountId,
                        CurrentBalance = balance
                    }
                };
            }
        }
    }