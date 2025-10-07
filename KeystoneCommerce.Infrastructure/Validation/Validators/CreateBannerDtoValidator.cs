using FluentValidation;
using KeystoneCommerce.Application.DTOs;
using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Infrastructure.Validation.Validators
{
    public class CreateBannerDtoValidator : AbstractValidator<CreateBannerDto>
    {
        public CreateBannerDtoValidator()
        {
            RuleFor(b => b.Title)
                .NotEmpty().
                WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

            RuleFor(b => b.Subtitle)
                .NotEmpty()
                .WithMessage("Subtitle is required.")
                .MaximumLength(500)
                .WithMessage("Subtitle must not exceed 500 characters.");

            RuleFor(b => b.Link)
                .NotEmpty()
                .WithMessage("Link is required.")
                .MaximumLength(100)
                .WithMessage("Link must not exceed 100 characters.");
               

            RuleFor(b => b.BannerType)
                .Must(value => Enum.IsDefined(typeof(BannerType), value))
                .WithMessage("Invalid Banner Type.");

            RuleFor(b => b.Priority)
                .GreaterThan(0)
                .WithMessage("Priority must be a positive integer.");

            RuleFor(b => b.Image)
                .NotNull()
                .WithMessage("Image is required.")
                .Must(image => image != null && image.Length > 0)
                .WithMessage("Image must not be empty.");

            RuleFor(b => b.ImageType)
                .NotEmpty()
                .WithMessage("Image type is required.");

            RuleFor(b => b.ImageUrl)
                .NotEmpty()
                .WithMessage("Image URL is required.");
        }
    }
}
