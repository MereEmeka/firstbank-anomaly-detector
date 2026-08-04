using System.Threading.Tasks;

namespace FirstBank.API.Services
{
    public interface IEmailService
    {
        Task SendFraudAlertAsync(string transactionId, string sourceAccountId, decimal amount, string flagReasons);
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}
