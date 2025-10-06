namespace KeystoneCommerce.Application.Common.Validation
{
    public class ApplicationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();

        public ApplicationValidationResult() { }

        public ApplicationValidationResult(bool isValid)
        {
            IsValid = isValid;
        }

        public static ApplicationValidationResult Success() => new ApplicationValidationResult(true);

        public static ApplicationValidationResult Failure(List<string> errors) =>
            new ApplicationValidationResult { IsValid = false, Errors = errors };
    }
}

