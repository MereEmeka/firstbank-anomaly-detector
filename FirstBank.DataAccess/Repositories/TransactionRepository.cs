using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using FirstBank.Core.Models;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace FirstBank.DataAccess.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly string _connectionString;

        public TransactionRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<decimal?> GetAccountBalanceAsync(Guid accountId)
        {
            using var connection = new SqlConnection(_connectionString);

            // We bypass the stored procedure entirely and just ask the Accounts table for the number
            string sql = "SELECT Balance FROM Accounts WHERE Id = @Id";

            // QuerySingleOrDefaultAsync executes the raw text query securely and returns the decimal
            var balance = await connection.QuerySingleOrDefaultAsync<decimal?>(
                sql,
                new { Id = accountId });

            return balance;
        }

        public async Task<string> SubmitTransactionAsync(Transaction transaction, bool isAnomaly, string idempotencyKey)
        {
            using var connection = new SqlConnection(_connectionString);

            // We execute the Stored Procedure using Dapper. 
            // No C# SqlTransaction needed—the SP handles its own rollbacks.
            try
            {
                await connection.ExecuteAsync("ExecuteTransfer", new
                {
                    Id = transaction.Id,
                    SourceAccountId = transaction.SourceAccountId,
                    DestinationAccountId = transaction.DestinationAccountId,
                    Amount = transaction.Amount,
                    Description = transaction.Description,
                    IsAnomaly = transaction.IsAnomalyFlagged,
                    IdempotencyKey = idempotencyKey
                }, commandType: CommandType.StoredProcedure);

                return transaction.Id.ToString();
            }
            catch (SqlException ex)
            {
                // If the SP throws our custom 50001, 50002, or 50003 errors, 
                // we catch them and translate them into C# exceptions
                if (ex.Number >= 50000)
                {
                    throw new InvalidOperationException(ex.Message);
                }

                // Let the global exception handler catch standard database crashes
                throw;
            }
        }
        public async Task<IEnumerable<AnomalyLog>> GetAnomalyLogsAsync(int pageNumber, int pageSize)
        {
            using var connection = new SqlConnection(_connectionString);

            int offset = (pageNumber - 1) * pageSize;

            //SQL Query for Pagination
            string sql = @"SELECT * FROM AnomalyLogs
                           ORDER BY LoggedAt DESC
                           OFFSET @Offset ROWS
                           FETCH NEXT @PageSize ROWS ONLY";

            //This passes the calculated variables safely into Dapper
            return await connection.QueryAsync<AnomalyLog>(sql, new
            {
                Offset = offset,
                PageSize = pageSize
            });
        }
    }
}