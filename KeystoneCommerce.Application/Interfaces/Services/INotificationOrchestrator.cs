namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface INotificationOrchestrator
    {
        Task SendAsync<TMessage>(TMessage message);
    }
}
