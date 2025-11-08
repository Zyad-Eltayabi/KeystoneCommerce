using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Application.Services;

namespace KeystoneCommerce.WebUI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register Application Services
            services.AddScoped<IBannerService, BannerService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IShopService, ShopService>();
            services.AddScoped<IProductDetailsService, ProductDetailsService>();
            services.AddScoped<IAccountService, AccountService>();
            return services;
        }
    }
}