namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IIdentityService
    {
        Task<List<string>> CreateUserAsync(string fullName, string email, string password);
    }
}
