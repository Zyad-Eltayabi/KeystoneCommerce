using FluentValidation;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Shared.Constants;

namespace KeystoneCommerce.Infrastructure.Validation.Validators.Product
{
    public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductDtoValidator()
        {
            Include(new BaseProductValidator());

            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Invalid proudct id");

            When(x => x.NewGalleries != null && x.NewGalleries.Count > 0, () =>
            {
                RuleFor(x => x.NewGalleries)
                    .Must(list => list.Count <= FileSizes.MaxNumberOfGalleryImages)
                    .WithMessage($"You can upload up to {FileSizes.MaxNumberOfGalleryImages} gallery images only.");
            });
        }

    }
}
