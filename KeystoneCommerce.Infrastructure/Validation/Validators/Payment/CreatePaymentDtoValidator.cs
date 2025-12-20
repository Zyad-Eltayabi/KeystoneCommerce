using FluentValidation;
using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Infrastructure.Validation.Validators.Payment
{
    public class CreatePaymentDtoValidator : AbstractValidator<CreatePaymentDto>
    {
        public CreatePaymentDtoValidator()
        {
            RuleFor(x => x.Provider)
                .IsInEnum()
                .WithMessage("Invalid payment provider type.");

            RuleFor(x => x.ProviderTransactionId)
                .MaximumLength(200)
                .WithMessage("Provider transaction ID must not exceed 200 characters.")
                .When(x => !string.IsNullOrEmpty(x.ProviderTransactionId));

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Payment amount must be greater than 0.");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Currency is required.")
                .MaximumLength(10)
                .WithMessage("Currency must not exceed 10 characters.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid payment status.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");

            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("Order ID must be greater than 0.");
        }
    }
}
