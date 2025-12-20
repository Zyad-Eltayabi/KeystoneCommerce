using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Application.Notifications.Contracts;
using KeystoneCommerce.Domain.Enums;
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
            return message.NotificationType switch
            {
                NotificationType.PasswordReset => await HandlePasswordResetNotificationAsync(message),
                NotificationType.OrderConfirmation => await HandleOrderConfirmationNotificationAsync(message),
                _ => false
            };
        }

        private async Task<bool> HandlePasswordResetNotificationAsync(EmailMessage message)
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

        private async Task<bool> HandleOrderConfirmationNotificationAsync(EmailMessage message)
        {
            var user = await _userManager.FindByIdAsync(message.To);

            if (user == null)
                return false;

            var orderNumber = message.Body;
            return await SendOrderConfirmationEmailAsync(user.Email!, user.FullName, orderNumber, message.Subject);
        }

        private async Task<bool> SendPasswordResetEmailAsync(string email, string fullName, string resetLink,
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

        private async Task<bool> SendOrderConfirmationEmailAsync(string email, string fullName, string orderNumber,
            string subject)
        {
            string body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; background:#f4f6f8; margin:0; padding:20px;'>
              <div style='max-width:600px; margin:auto; background:#fff; border-radius:8px; overflow:hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
                
                <!-- Header -->
                <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding:30px 20px; text-align:center;'>
                  <h1 style='color:#fff; margin:0; font-size:28px;'>
                    <i style='font-size:36px;'>✓</i><br/>
                    Order Confirmed!
                  </h1>
                </div>
                
                <!-- Content -->
                <div style='padding:30px;'>
                  <p style='font-size:16px; color:#555; margin-top:0;'>Hi {fullName},</p>
                  
                  <p style='font-size:16px; color:#555;'>
                    Thank you for your order! We're happy to confirm that we've received your order and it's being processed.
                  </p>
                  
                  <!-- Order Number Box -->
                  <div style='background:#f8f9fa; border-left:4px solid #28a745; padding:20px; margin:20px 0; border-radius:4px;'>
                    <p style='margin:0; color:#666; font-size:14px; text-transform:uppercase; letter-spacing:1px;'>Order Number</p>
                    <p style='margin:5px 0 0 0; color:#333; font-size:24px; font-weight:bold;'>{orderNumber}</p>
                  </div>
                  
                  <p style='font-size:16px; color:#555;'>
                    You'll receive another email once your order has been shipped with tracking information.
                  </p>
                  
                  <!-- Action Button -->
                  <div style='text-align:center; margin:30px 0;'>
                    <a href='{_configuration["AppSettings:BaseUrl"]}/Shop' 
                       style='display:inline-block; background:#28a745; color:#fff; padding:14px 32px; border-radius:6px; text-decoration:none; font-weight:bold; font-size:16px;'>
                      Continue Shopping
                    </a>
                  </div>
                  
                  <!-- Divider -->
                  <hr style='border:none; border-top:1px solid #e9ecef; margin:30px 0;'/>
                  
                  <!-- Support Info -->
                  <div style='background:#f8f9fa; padding:20px; border-radius:6px; margin-top:20px;'>
                    <h3 style='margin:0 0 10px 0; color:#333; font-size:16px;'>Need Help?</h3>
                    <p style='margin:0; color:#666; font-size:14px;'>
                      If you have any questions about your order, please don't hesitate to contact our support team.
                    </p>
                  </div>
                </div>
                
                <!-- Footer -->
                <div style='background:#f8f9fa; padding:20px; text-align:center; border-top:1px solid #e9ecef;'>
                  <p style='font-size:12px; color:#999; margin:0;'>
                    &copy; {DateTime.UtcNow.Year} KeystoneCommerce. All rights reserved.
                  </p>
                  <p style='font-size:12px; color:#999; margin:10px 0 0 0;'>
                    This is an automated email. Please do not reply to this message.
                  </p>
                </div>
                
              </div>
            </body>
            </html>";

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
