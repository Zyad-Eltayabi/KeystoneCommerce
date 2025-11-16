namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface INotificationService<TMessage>
    {
        Task<bool> SendNotificationAsync(TMessage message);
    }
}
