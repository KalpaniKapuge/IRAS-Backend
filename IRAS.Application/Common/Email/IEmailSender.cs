// IRAS.Application/Common/Email/IEmailSender.cs
namespace IRAS.Application.Common.Email
{
    // Same pattern as IJdGenerator/IFileStorage: the interface is the real contract,
    // the implementation is swappable. LogEmailSender is the safe default for local/dev
    // (no SMTP credentials required); a production SmtpEmailSender or SendGridEmailSender
    // can implement this same interface later without touching any calling code.
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string body, CancellationToken ct);
    }
}
