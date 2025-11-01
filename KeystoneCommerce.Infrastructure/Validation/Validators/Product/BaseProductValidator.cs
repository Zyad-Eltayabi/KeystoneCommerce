using FluentValidation;
using KeystoneCommerce.Application.DTOs.Product;

namespace KeystoneCommerce.Infrastructure.Validation.Validators.Product
{
    public class BaseProductValidator : AbstractValidator<BaseProductDto>
    {
        public BaseProductValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Summary)
                .NotEmpty().WithMessage("Summary is required.")
                .MaximumLength(500).WithMessage("Summary cannot exceed 500 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0.");

            RuleFor(x => x.Discount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Discount cannot be negative.")
                .When(x => x.Discount.HasValue);

            RuleFor(x => x.QTY)
                .GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative.");

            RuleFor(x => x.Tags)
                .MaximumLength(1000).WithMessage("Tags cannot exceed 1000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Tags));
        }
    }
}
