using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Application.Notifications.Contracts;
using KeystoneCommerce.Infrastructure.Helpers;
using KeystoneCommerce.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace KeystoneCommerce.Infrastructure.Services
{
    public class EmailService : INotificationService<EmailMessage>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly EmailSettings _settings;

        public EmailService(UserManager<ApplicationUser> userManager, IConfiguration configuration, IOptions<EmailSettings> settings)
        {
            _userManager = userManager;
            _configuration = configuration;
            _settings = settings.Value;
        }

        public async Task<bool> SendNotificationAsync(EmailMessage message)
        {
            var user = await _userManager.FindByEmailAsync(message.To);

            if (user == null)
                return false;

            // Generate a unique, secure token for password reset
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Encode the token so it can be safely used in a URL
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Construct the password reset link with the encoded token and user’s email
            var baseUrl = _configuration["AppSettings:BaseUrl"];
            var resetLink = $"{baseUrl}/Account/ResetPassword?email={user.Email}&token={encodedToken}";

            // Send the reset link via email to the user
            return await SendPasswordResetEmailAsync(user.Email!, user.FullName, resetLink, message.Subject);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string fullName, string resetLink,
            string subject)
        {
            string body = $@"
            <html><body style='font-family: Arial, sans-serif; background:#f4f6f8; margin:0; padding:20px;'>
              <div style='max-width:600px; margin:auto; background:#fff; padding:30px; border-radius:8px;'>
                <h2 style='color:#333;'>Password Reset Request</h2>
                <p style='font-size:16px; color:#555;'>Hi {fullName},</p>
                <p style='font-size:16px; color:#555;'>We received a request to reset your password. Click the button below to choose a new one.</p>
                <p style='text-align:center;'>
                  <a href='{resetLink}' style='background:#0d6efd; color:#fff; padding:12px 24px; border-radius:6px; text-decoration:none; font-weight:bold;'>Reset Password</a>
                </p>
                <p style='font-size:13px; color:#777;'>If you didn't request this, you can ignore this email.</p>
                <p style='font-size:12px; color:#999; margin-top:30px;'>&copy; {DateTime.UtcNow.Year} KeystoneCommerce. All rights reserved.</p>
              </div>
            </body></html>";

            return await SendEmailAsync(email, subject, body);
        }

        private async Task<bool> SendEmailAsync(string email, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(
                    _settings.From,
                    Environment.GetEnvironmentVariable("SMTP_APP_PASSWORD")
                );
                var mail = new MailMessage
                {
                    From = new MailAddress(_settings.From),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(email);
                await Task.Run(() => client.Send(mail));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email Error: " + ex.Message);
                return false;
            }
        }

    }
}
