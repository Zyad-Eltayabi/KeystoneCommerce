using KeystoneCommerce.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KeystoneCommerce.Application.Services
{
    public class NotificationService : INotificationOrchestrator
    {
        private readonly IServiceProvider _provider;

        public NotificationService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public Task SendAsync<TMessage>(TMessage message)
        {
            var service = _provider.GetRequiredService<INotificationService<TMessage>>();
            return service.SendNotificationAsync(message);
        }
    }
}
