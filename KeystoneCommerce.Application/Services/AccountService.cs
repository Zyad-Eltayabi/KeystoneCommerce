using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Account;
using KeystoneCommerce.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IIdentityService _identityService;
        private readonly ILogger<AccountService> _logger;
        private readonly IApplicationValidator<RegisterDto> _validator;

        public AccountService(IIdentityService identityService, ILogger<AccountService> logger, IApplicationValidator<RegisterDto> validator)
        {
            _identityService = identityService;
            _logger = logger;
            _validator = validator;
        }

        public async Task<Result<RegisterDto>> RegisterAsync(RegisterDto registerDto)
        {
            _logger.LogInformation("Registering user with email: {Email}", registerDto.Email);
            var validationResult = _validator.Validate(registerDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for user registration with email: {Email}. Errors: {Errors}", registerDto.Email, string.Join(", ", validationResult.Errors));
                return Result<RegisterDto>.Failure(validationResult.Errors);
            }
            return await CompleteRegistrationAsync(registerDto);
        }

        private async Task<Result<RegisterDto>> CompleteRegistrationAsync(RegisterDto registerDto)
        {
            List<string> result = await _identityService.CreateUserAsync(registerDto.FullName, registerDto.Email, registerDto.Password);
            if (result.Any())
            {
                _logger.LogWarning("User registration failed for email: {Email}. Errors: {Errors}", registerDto.Email, string.Join(", ", result));
                return Result<RegisterDto>.Failure(result);
            }
            _logger.LogInformation("User registered successfully with email: {Email}", registerDto.Email);
            return Result<RegisterDto>.Success(registerDto);
        }
    }
}
