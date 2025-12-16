using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Application.Services;
using KeystoneCommerce.WebUI.Services;

namespace KeystoneCommerce.WebUI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            ConfigureApplicationServices(services);
            ConfigureWebService(services);
            return services;
        }

        private static void ConfigureWebService(IServiceCollection services)
        {
            services.AddScoped<CartCookieService>();
            services.AddScoped<CartService>();
        }

        private static void ConfigureApplicationServices(IServiceCollection services)
        {
            // Register Application Services
            services.AddScoped<IBannerService, BannerService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IShopService, ShopService>();
            services.AddScoped<IProductDetailsService, ProductDetailsService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddTransient<INotificationOrchestrator, NotificationService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<ICouponService, CouponService>();
            services.AddScoped<IShippingMethodService, ShippingMethodService>();
            services.AddScoped<ICheckoutService, CheckoutService>();
            services.AddScoped<IShippingAddressService, ShippingAddressService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
            services.AddScoped<IInventoryReservationService, InventoryReservationService>();
        }
    }
}