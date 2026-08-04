using FirstBank.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FirstBank.API.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendFraudAlertAsync(string transactionId, string sourceAccountId, decimal Amount, string flagReasons)
        {
            var senderEmail = _config["EmailSettings:SenderEmail"];

            string body = $@"
                <h2>FirstBank Automated Security Engine</h2>
                <p><strong>Status:</strong> TRANSFER BLOCKED</p>
                <p><strong>Transaction ID:</strong> {transactionId}</p>
                <p><strong>Source Account:</strong> {sourceAccountId}</p>
                <p><strong>Attempted Amount:</strong> {Amount:N2} NGN</p>
                <hr />
                <h3>Anomaly Triggers:</h3>
                <p style='color: red;'>{flagReasons}</p>
                <br/>
                <p>Immediate administrative review is recommended.</p>";

            await SendEmailAsync(senderEmail!, $"CRITICAL SECURITY ALERT: Fraud Blocked [{transactionId}]", body);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var smtpServer = _config["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"]!);
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var appPassword = _config["EmailSettings:AppPassword"];
                var senderName = _config["EmailSettings:SenderName"];

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail!, senderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            }
        }
    }
}
