using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace KeystoneCommerce.WebUI.Attributes
{
    public class ValidBannerTypeAttribute : ValidationAttribute
    {
        private readonly List<int> _bannerTypesValues;
        public ValidBannerTypeAttribute()
        {
            _bannerTypesValues = new List<int> { 1, 2, 3 };
        }
        protected override ValidationResult? IsValid(object value, ValidationContext validationContext)
        {
            if (value is not int intValue)
            {
                return new ValidationResult("Invalid Banner Type");
            }

            if (!_bannerTypesValues.Contains(intValue))
            {
                return new ValidationResult("The value must be between 1 and 3");
            }

            return ValidationResult.Success;
        }
    }
}
