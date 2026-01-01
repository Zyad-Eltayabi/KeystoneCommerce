using KeystoneCommerce.Application.DTOs.Account;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IAccountService
    {
        Task<Result<RegisterDto>> RegisterAsync(RegisterDto registerDto);
        Task<Result<RegisterDto>> LoginAsync(LoginDto loginDto);
        Task<bool> LogoutAsync();
        Task<bool> SendPasswordResetLinkAsync(string email);
        Task<Result<string>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    }
}
