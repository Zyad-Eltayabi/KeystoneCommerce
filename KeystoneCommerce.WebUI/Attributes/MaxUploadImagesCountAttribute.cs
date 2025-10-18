using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace KeystoneCommerce.WebUI.Attributes
{
    /// <summary>
    /// Validates that the number of uploaded images does not exceed a defined maximum.
    /// </summary>
    public class MaxUploadImagesCountAttribute : ValidationAttribute
    {
        private readonly int _maxImages;

        public MaxUploadImagesCountAttribute(int maxImages)
        {
            _maxImages = maxImages;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile[] files && files.Length > _maxImages)
            {
                return new ValidationResult($"You can upload up to {_maxImages} images only.");
            }

            return ValidationResult.Success;
        }
    }
}
