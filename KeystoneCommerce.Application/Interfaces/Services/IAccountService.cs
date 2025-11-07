using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Account;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IAccountService
    {
        Task<Result<RegisterDto>> RegisterAsync(RegisterDto registerDto);
        Task<Result<RegisterDto>> LoginAsync(LoginDto loginDto);
    }
}
