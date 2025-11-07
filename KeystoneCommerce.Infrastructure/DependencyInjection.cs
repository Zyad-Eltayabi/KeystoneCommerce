using FluentValidation;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Infrastructure.Persistence.Data;
using KeystoneCommerce.Infrastructure.Persistence.Identity;
using KeystoneCommerce.Infrastructure.Profiles;
using KeystoneCommerce.Infrastructure.Repositories;
using KeystoneCommerce.Infrastructure.Services;
using KeystoneCommerce.Infrastructure.Validation;
using KeystoneCommerce.Infrastructure.Validation.Validators.Banner;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Filters;

namespace KeystoneCommerce.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostBuilder host)
        {
            AddApplicationDbContext(services, configuration);
            AddIdentityConfiguration(services);
            ConfigureSerilogForHost(host);
            AddFluentValidationServices(services);
            RegisterRepositoryServices(services);
            RegisterInfrastructureServices(services);
            AddAutoMapperServices(services);
            return services;
        }

        private static void AddAutoMapperServices(IServiceCollection services)
        {
            services.AddAutoMapper(e => e.AddProfile<InfrastructureMappings>());
        }

        private static void RegisterInfrastructureServices(IServiceCollection services)
        {
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IMappingService, MappingService>();
            services.AddScoped<IIdentityService, IdentityService>();
        }

        private static void RegisterRepositoryServices(IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IBannerRepository, BannerRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IShopRepository, ShopRepository>();
            services.AddScoped<IProductDetailsRepository, ProductDetailsRepository>();
        }

        private static void AddFluentValidationServices(IServiceCollection services)
        {
            // Register FluentValidation validators
            // it will be register all validators in the assembly where CreateUserDtoValidator is located
            services.AddValidatorsFromAssemblyContaining<CreateBannerDtoValidator>();

            // Register the FluentValidationAdapter as the implementation for IApplicationValidator<T>
            services.AddScoped(typeof(IApplicationValidator<>), typeof(FluentValidationAdapter<>));
        }

        private static void ConfigureSerilogForHost(IHostBuilder host)
        {
            host.UseSerilog((context, services, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .Filter.ByExcluding(Matching.WithProperty<string>("RequestPath", path =>
                        path.StartsWith("/assets") ||
                        path.StartsWith("/css") ||
                        path.StartsWith("/js") ||
                        path.StartsWith("/img") ||
                        path.StartsWith("/fonts") ||
                        path.Contains(".woff") ||
                        path.Contains(".ico")));
            });
        }

        private static void AddIdentityConfiguration(IServiceCollection services)
        {
            services.Configure<SecurityStampValidatorOptions>(opts =>
            {
                // Enables immediate logout, after updating the user's security stamp.
                opts.ValidationInterval = TimeSpan.FromHours(1);
            });
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "KeystoneCommerce";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(2);
                options.SlidingExpiration = true;
                options.LoginPath = "/Account/Login";
            });

            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        }

        private static void AddApplicationDbContext(IServiceCollection services, IConfiguration configuration)
        {
            // Add database context using SQL Server.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });
        }
    }
}

