using System.Net;
using System.Net.Mail;
using nia_api.Models;

namespace nia_api.Services;

public class EmaiSenderService : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmaiSenderService> _logger;

    public EmaiSenderService(IConfiguration configuration, ILogger<EmaiSenderService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task SendEmailAsync(string to, string subject, string firstName, string lastName, string verificationCode)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("Smtp");
            var smtpClient = new SmtpClient(smtpSettings["Host"])
            {
                Port = int.Parse(smtpSettings["Port"]),
                Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                EnableSsl = true,
            };

            // Bypass the SSL certificate validation (not recommended for production)
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            string emailBody = GenerateEmailBody(firstName, lastName, verificationCode);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["Username"]),
                Subject = subject,
                Body = emailBody,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation($"Email sent to {to} successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending email to {to}: {ex.Message}");
            throw;
        }
    }

    private string GenerateEmailBody(string firstName, string lastName, string verificationCode)
    {
        return $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Registration Confirmation</title>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f4f4;
                    margin: 0;
                    padding: 0;
                }}
                .container {{
                    width: 80%;
                    margin: 20px auto;
                    background-color: #ffffff;
                    padding: 20px;
                    box-shadow: 0 0 10px rgba(0,0,0,0.1);
                }}
                .header {{
                    text-align: center;
                    padding-bottom: 20px;
                }}
                .header h1 {{
                    margin: 0;
                }}
                .content {{
                    margin-top: 20px;
                }}
                .footer {{
                    margin-top: 20px;
                    text-align: center;
                    font-size: 12px;
                    color: #888;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Registration Confirmation</h1>
                </div>
                <div class='content'>
                    <p>Dear {firstName} {lastName},</p>
                    <p>Thank you for registering with us! To complete your registration, please use the following verification code:</p>
                    <p><strong>{verificationCode}</strong></p>
                    <p>If you did not register with us, please ignore this email.</p>
                    <p>Thank you!</p>
                </div>
                <div class='footer'>
                    <p>&copy; {DateTime.Now.Year} Your Company Name. All rights reserved.</p>
                    <p>Your Company Address</p>
                </div>
            </div>
        </body>
        </html>";
    }
}