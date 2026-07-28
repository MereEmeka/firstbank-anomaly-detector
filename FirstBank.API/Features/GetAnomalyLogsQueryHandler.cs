using MediatR;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Repositories;

namespace FirstBank.API.Features
{
    public class GetAnomalyLogsQueryHandler : IRequestHandler<GetAnomalyLogsQuery, IEnumerable<AnomalyLog>>
    {
        private ITransactionRepository _repository;

        //This handler only injects exactly what it needs for this specific task
        public GetAnomalyLogsQueryHandler(ITransactionRepository repository)
        {
            _repository = repository;
        }
        public async Task <IEnumerable<AnomalyLog>> Handle(GetAnomalyLogsQuery request, CancellationToken cancellationToken)
        {
            //We read parameters sent inside the request object
            return await _repository.GetAnomalyLogsAsync(request.PageNumber, request.PageSize);
        }
    }
}
