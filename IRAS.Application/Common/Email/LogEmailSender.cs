// IRAS.Application/Common/Email/LogEmailSender.cs
using Microsoft.Extensions.Logging;

namespace IRAS.Application.Common.Email
{
    public class LogEmailSender : IEmailSender
    {
        private readonly ILogger<LogEmailSender> _logger;
        public LogEmailSender(ILogger<LogEmailSender> logger) => _logger = logger;

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
        {
            _logger.LogInformation("[DEV EMAIL] To: {ToEmail} | Subject: {Subject}\n{Body}", toEmail, subject, body);
            return Task.CompletedTask;
        }
    }
}
