using FluentValidation;
using KeystoneCommerce.Application.Common.Validation;

namespace KeystoneCommerce.Infrastructure.Validation
{
    public class FluentValidationAdapter<T> : IApplicationValidator<T>
    {
        private readonly IValidator<T> _fluentValidator;

        public FluentValidationAdapter(IValidator<T> fluentValidator)
        {
            _fluentValidator = fluentValidator;
        }

        public async Task<ApplicationValidationResult> ValidateAsync(T instance)
        {
            // Directly return FluentValidation's result - no conversion!
            var result = await _fluentValidator.ValidateAsync(instance);
            return MapToApplicationValidationResult(result);
        }

        private static ApplicationValidationResult MapToApplicationValidationResult(FluentValidation.Results.ValidationResult result)
        {
            return new ApplicationValidationResult
            {
                IsValid = result.IsValid,
                Errors = result.Errors.Select(e => e.ErrorMessage).ToList()
            };
        }

        public ApplicationValidationResult Validate(T instance)
        {
            // Directly return FluentValidation's result - no conversion!
            var result = _fluentValidator.Validate(instance);
            return MapToApplicationValidationResult(result);
        }
    }
}
