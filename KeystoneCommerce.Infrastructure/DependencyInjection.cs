using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Infrastructure.Repositories;
using KeystoneCommerce.Infrastructure.Services;
using KeystoneCommerce.Infrastructure.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace KeystoneCommerce.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Register FluentValidation validators
            // it will be register all validators in the assembly where CreateUserDtoValidator is located
            //services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();

            // Register the FluentValidationAdapter as the implementation for IApplicationValidator<T>
            services.AddScoped(typeof(IApplicationValidator<>), typeof(FluentValidationAdapter<>));

            // Register Generic Repository and Specific Repositories
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IBannerRepository, BannerRepository>();

            // Register other infrastructure services
            services.AddScoped<IImageService, ImageService>();

            return services;
        }
    }
}

