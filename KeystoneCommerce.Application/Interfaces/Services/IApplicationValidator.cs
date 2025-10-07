using KeystoneCommerce.Application.Common.Validation;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IApplicationValidator<in T>
    {
        Task<ApplicationValidationResult> ValidateAsync(T instance);
        ApplicationValidationResult Validate(T instance);
    }
}
