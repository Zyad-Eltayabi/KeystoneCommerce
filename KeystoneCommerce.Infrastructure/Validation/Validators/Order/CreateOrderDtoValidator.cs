using FluentValidation;
using KeystoneCommerce.Application.DTOs.Order;

namespace KeystoneCommerce.Infrastructure.Validation.Validators.Order
{
    public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
    {
        public CreateOrderDtoValidator()
        {
            RuleFor(x => x.ShippingMethod)
                .NotEmpty()
                .WithMessage("Shipping method is required.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.");

            RuleFor(x => x.ShippingDetails)
                .NotNull()
                .WithMessage("Shipping details are required.");

            RuleFor(x => x.ProductsWithQuantity)
                .NotNull()
                .WithMessage("Products with quantity is required.")
                .Must(x => x != null && x.Count > 0)
                .WithMessage("At least one product must be included in the order.");

            RuleForEach(x => x.ProductsWithQuantity)
                .ChildRules(item =>
                {
                    item.RuleFor(x => x.Key)
                        .GreaterThan(0)
                        .WithMessage("Product ID must be greater than 0.");

                    item.RuleFor(x => x.Value)
                        .GreaterThan(0)
                        .WithMessage("Product quantity must be greater than 0.");
                });
        }
    }
}
