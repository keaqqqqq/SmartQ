using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using FNBReservation.Modules.Authentication.Core.Interfaces;

namespace FNBReservation.Infrastructure.Services.Notification
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetToken)
        {
            try
            {
                _logger.LogInformation($"Attempting to send email to {email} with SMTP settings: Host={_configuration["SmtpSettings:Host"]}, Port={_configuration["SmtpSettings:Port"]}, SSL={_configuration["SmtpSettings:EnableSsl"]}");

                // Load SMTP settings from configuration
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"]);
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["FromEmail"];
                var frontendUrl = _configuration["FrontendUrl"];

                // Validate minimal required configuration
                if (string.IsNullOrEmpty(host))
                {
                    _logger.LogError("SMTP host is not configured. Check your appsettings.json file.");
                    return;
                }

                if (string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogError("FromEmail is not configured. Check your appsettings.json file.");
                    return;
                }

                // For MailHog we don't need these
                // We still log a warning but don't treat them as errors
                if (string.IsNullOrEmpty(frontendUrl))
                {
                    _logger.LogWarning("FrontendUrl is not configured. Using a placeholder URL for password reset links.");
                    frontendUrl = "http://localhost:5002"; // Default fallback
                }

                // Create reset link
                var resetLink = $"{frontendUrl}/reset-password?token={WebUtility.UrlEncode(resetToken)}";

                // Create email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("FNB Reservation System", fromEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = "Password Reset Request";

                // Create HTML body
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                                .container {{ padding: 20px; max-width: 600px; margin: 0 auto; }}
                                .button {{ background-color: #4CAF50; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <h2>Reset Your Password</h2>
                                <p>We received a request to reset your password. Please click the button below to reset it:</p>
                                <p><a href='{resetLink}' class='button'>Reset Password</a></p>
                                <p>If you didn't request a password reset, you can ignore this email.</p>
                                <p>The link will expire in 24 hours.</p>
                                <p>If you're having trouble with the button above, copy and paste the following link into your browser:</p>
                                <p>{resetLink}</p>
                            </div>
                        </body>
                        </html>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                // Send email using MailKit
                using (var client = new SmtpClient())
                {
                    // For development/testing environments, you may need to disable SSL validation
                    if (_configuration.GetValue<bool>("SmtpSettings:DisableCertificateValidation", false))
                    {
                        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    }

                    await client.ConnectAsync(host, port, SecureSocketOptions.Auto);

                    // Only authenticate if credentials are provided
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        await client.AuthenticateAsync(username, password);
                    }
                    _logger.LogInformation($"Successfully connected to SMTP server");
                    await client.SendAsync(message);
                    _logger.LogInformation($"Email sent successfully to {email}");
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Password reset email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password reset email to {email}: {ex.Message}");
                throw; // Rethrow to let the calling service handle it
            }
        }
    }
}