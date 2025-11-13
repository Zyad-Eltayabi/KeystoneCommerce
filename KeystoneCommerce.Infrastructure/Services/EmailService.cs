using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Application.Notifications.Contracts;

namespace KeystoneCommerce.Infrastructure.Services
{
    public class EmailService : INotificationService<EmailMessage>
    {
        public Task SendNotificationAsync(EmailMessage message)
        {
            Console.WriteLine("successfully sent email !");
            return Task.CompletedTask;
        }
    }
}
