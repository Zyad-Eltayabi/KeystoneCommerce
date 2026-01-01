using KeystoneCommerce.Application.DTOs.Account;
using KeystoneCommerce.Application.DTOs.Order;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IIdentityService
    {
        Task<List<string>> CreateUserAsync(string fullName, string email, string password);
        Task<bool> LoginUserAsync(string email, string password,bool rememberMe);
        Task<bool> LogoutUserAsync();
        Task<bool> IsUserExists(string email);
        Task<bool> IsUserExistsById(string userId);
        Task<List<string>> ResetPasswordAsync(string email, string token, string newPassword);
        Task<UserBasicInfoDto?> GetUserBasicInfoByIdAsync(string userId);
    }
}
