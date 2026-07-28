using FirstBank.Core.Models;

namespace FirstBank.DataAccess.Repositories
{
    public interface ITransactionRepository
    {
        Task<decimal?> GetAccountBalanceAsync(Guid accountId);
        Task<string> SubmitTransactionAsync(Transaction transaction, bool isAnomaly, string idempotencyKey);
        Task<IEnumerable<AnomalyLog>> GetAnomalyLogsAsync(int pageNumber, int pageSize);
    }
}