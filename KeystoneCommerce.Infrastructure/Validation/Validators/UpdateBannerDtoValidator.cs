using FluentValidation;
using KeystoneCommerce.Application.DTOs;
using KeystoneCommerce.Application.DTOs.Banner;
using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Infrastructure.Validation.Validators;

public class UpdateBannerDtoValidator : AbstractValidator<UpdateBannerDto>
{
    public UpdateBannerDtoValidator()
    {
        RuleFor(b => b.Id)
            .GreaterThan(0)
            .WithMessage("Invalid Banner Id.");
            
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
        
        When(banner => banner.Image is not null && banner.Image.Length > 0, () =>
        {
            RuleFor(x => x.ImageType)
                .NotEmpty()
                .WithMessage("Invalid image type.");
            
            RuleFor(x => x.ImageUrl)
                .NotEmpty()
                .WithMessage("Invalid image.");
        });
    }
}