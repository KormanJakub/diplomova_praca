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
    
    public async Task SendEmailAsync(string to, string subject, string firstName, string lastName, string verificationCode, string typeOfEmail)
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

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            var registrationBody = GenerateRegistrionEmailBody(firstName, lastName, verificationCode);
            var verificationBody = GenerateVerificationEmailBody(firstName, lastName, verificationCode);
            string body = "";

            if (typeOfEmail == "Registration")
                body = registrationBody;

            if (typeOfEmail == "Verification")
                body = verificationBody;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["Username"]),
                Subject = subject,
                Body = body,
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

    private string GenerateRegistrionEmailBody(string firstName, string lastName, string verificationCode)
    {
        return $@"
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
                    <h1>Verifikácia účtu</h1>
                </div>
                <div class='content'>
                    <p>{firstName} {lastName},</p>
                    <p>Ďakujeme, že ste sa zaregistrovali do nášho Eshopu. Pre pokračovanie zadajte verifikačný kod:</p>
                    <p><strong>{verificationCode}</strong></p>
                    <p>Ak ste sa neregistrovali, ignorujte túto správu</p>
                    <p>Ďakujem!</p>
                </div>
                <div class='footer'>
                    <p>&copy; {DateTime.Now.Year} NIA. All rights reserved.</p>
                </div>
            </div>
        </body>
        </html>";
    }
    
    private string GenerateVerificationEmailBody(string firstName, string lastName, string verificationCode)
    {
        return $@"
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
                    <h1>Zabudnutie hesla</h1>
                </div>
                <div class='content'>
                    <p>{firstName} {lastName},</p>
                    <p>Pre následovné prihlásenie vám posielame verifikačný kód. Pre pokračovanie zadajte verifikačný kod:</p>
                    <p><strong>{verificationCode}</strong></p>
                    <p>Ak ste sa neposlali žiadosť o verifikáciu, ignorujte daný email!</p>
                    <p>Ďakujem!</p>
                </div>
                <div class='footer'>
                    <p>&copy; {DateTime.Now.Year} NIA. All rights reserved.</p>
                </div>
            </div>
        </body>
        </html>";
    }
}