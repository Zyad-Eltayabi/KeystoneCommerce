using System.ComponentModel.DataAnnotations;

public class AllowedExtensionsAttribute : ValidationAttribute
{
    private readonly string _extensions;
    public AllowedExtensionsAttribute(string extensions)
    {
        _extensions = extensions;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName);
            bool isValid = _extensions.Split(",").Contains(extension, StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(extension) || !isValid )
            {
                return new ValidationResult(ErrorMessage ?? "Invalid file type.");
            }
        }

        return ValidationResult.Success;
    }
}
