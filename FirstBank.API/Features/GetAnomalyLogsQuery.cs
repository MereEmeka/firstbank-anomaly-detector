using MediatR;
using FirstBank.Core.Models;
namespace FirstBank.API.Features
{
    //We implement IRequest<T>, telling MediatR that this request
    //expects a list of AnomalyLogs back as a response.
    public record GetAnomalyLogsQuery(int PageNumber, int PageSize) : IRequest<IEnumerable<AnomalyLog>>;
}
