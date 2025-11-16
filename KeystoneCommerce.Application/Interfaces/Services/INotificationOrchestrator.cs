namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface INotificationOrchestrator
    {
        Task<bool> SendAsync<TMessage>(TMessage message);
    }
}
