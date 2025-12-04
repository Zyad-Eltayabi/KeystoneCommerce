using FluentValidation;
using KeystoneCommerce.Application.DTOs.ShippingDetails;

namespace KeystoneCommerce.Infrastructure.Validation.Validators.ShippingAddress
{
    public class CreateShippingDetailsDtoValidator : AbstractValidator<CreateShippingDetailsDto>
    {
        public CreateShippingDetailsDtoValidator()
        {
            RuleFor(x => x.FullName.Trim())
                .NotEmpty()
                .WithMessage("Full name is required.")
                .MaximumLength(200)
                .WithMessage("Full name cannot exceed 200 characters.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Invalid email address.")
                .MaximumLength(256)
                .WithMessage("Email cannot exceed 256 characters.");

            RuleFor(x => x.Address.Trim())
                .NotEmpty()
                .WithMessage("Address is required.")
                .MaximumLength(500)
                .WithMessage("Address cannot exceed 500 characters.");

            RuleFor(x => x.City.Trim())
                .NotEmpty()
                .WithMessage("City is required.")
                .MaximumLength(100)
                .WithMessage("City cannot exceed 100 characters.");

            RuleFor(x => x.Country.Trim())
                .NotEmpty()
                .WithMessage("Country is required.")
                .MaximumLength(100)
                .WithMessage("Country cannot exceed 100 characters.");

            RuleFor(x => x.Phone)
                .NotEmpty()
                .WithMessage("Phone is required.")
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .WithMessage("Invalid phone number.")
                .MaximumLength(20)
                .WithMessage("Phone cannot exceed 20 characters.");

            RuleFor(x => x.PostalCode)
                .MaximumLength(20)
                .WithMessage("Postal code cannot exceed 20 characters.")
                .When(x => !string.IsNullOrEmpty(x.PostalCode));
        }
    }
}
