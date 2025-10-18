using FluentValidation;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Shared.Constants;

namespace KeystoneCommerce.Infrastructure.Validation.Validators.Product
{
    public class CreateProductValidator : AbstractValidator<CreateProductDto>
    {
        public CreateProductValidator()
        {
            Include(new BaseProductValidator());

            RuleFor(x => x.MainImage)
              .NotNull()
              .WithMessage("Main image is required.")
              .DependentRules(() =>
              {
                  RuleFor(x => x.MainImage.Data)
                      .NotEmpty().WithMessage("Main image data cannot be empty.");

                  RuleFor(x => x.MainImage.Type)
                      .NotEmpty().WithMessage("Main image type is required.");
              });


            RuleFor(x => x.Gallaries)
             .NotNull()
             .WithMessage("Gallaries cannot be null.")
             .NotEmpty()
             .WithMessage("At least one gallery image is required.")
               .Must(list => list.Count <= FileSizes.MaxNumberOfGalleryImages)
               .WithMessage($"You can upload a maximum of {FileSizes.MaxNumberOfGalleryImages} images.")
             .DependentRules(() =>
             {
                 RuleForEach(x => x.Gallaries)
                     .ChildRules(gallery =>
                     {
                         gallery.RuleFor(g => g.Data)
                             .NotEmpty().WithMessage("Gallery image data cannot be empty.");

                         gallery.RuleFor(g => g.Type)
                             .NotEmpty().WithMessage("Gallery image type is required.");
                     });
             });

        }
    }
}
