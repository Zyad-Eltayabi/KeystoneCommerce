using KeystoneCommerce.Application.DTOs.Home;

namespace KeystoneCommerce.Application.Interfaces.Services;

public interface IHomeService
{
    Task<HomePageDto> GetHomePageDataAsync();
}
