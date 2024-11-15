using nia_api.Enums;

namespace nia_api.Models;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string firstName, string lastName, string verificationCode, EEmail emailType);
}