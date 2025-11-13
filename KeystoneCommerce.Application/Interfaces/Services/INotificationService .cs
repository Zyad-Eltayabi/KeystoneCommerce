namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface INotificationService<TMessage>
    {
        Task SendNotificationAsync(TMessage message);
    }
}
