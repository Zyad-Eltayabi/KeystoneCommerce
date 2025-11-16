using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Account;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Application.Notifications.Contracts;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IIdentityService _identityService;
        private readonly ILogger<AccountService> _logger;
        private readonly IApplicationValidator<RegisterDto> _registerValidator;
        private readonly IApplicationValidator<LoginDto> _loginValidator;
        private readonly INotificationOrchestrator _notificationOrchestrator;

        public AccountService(IIdentityService identityService, ILogger<AccountService> logger,
            IApplicationValidator<RegisterDto> validator,
            IApplicationValidator<RegisterDto> registerValidator,
            IApplicationValidator<LoginDto> loginValidator,
            INotificationOrchestrator notificationService)
        {
            _identityService = identityService;
            _logger = logger;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _notificationOrchestrator = notificationService;
        }

        public async Task<Result<RegisterDto>> RegisterAsync(RegisterDto registerDto)
        {
            _logger.LogInformation("Registering user with email: {Email}", registerDto.Email);
            var validationResult = _registerValidator.Validate(registerDto);
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

        public async Task<Result<RegisterDto>> LoginAsync(LoginDto loginDto)
        {
            _logger.LogInformation("Login attempt for user with email: {Email}", loginDto.Email);
            var validationResult = _loginValidator.Validate(loginDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for user login with email: {Email}. Errors: {Errors}", loginDto.Email, string.Join(", ", validationResult.Errors));
                return Result<RegisterDto>.Failure("Invalid email or password.");
            }
            return await CompleteLoginAsync(loginDto);
        }

        private async Task<Result<RegisterDto>> CompleteLoginAsync(LoginDto loginDto)
        {
            var result = await _identityService.LoginUserAsync(loginDto.Email, loginDto.Password, loginDto.RememberMe);
            if (!result)
            {
                _logger.LogWarning("User login failed for email: {Email}", loginDto.Email);
                return Result<RegisterDto>.Failure("Invalid email or password.");
            }
            _logger.LogInformation("User logged in successfully with email: {Email}", loginDto.Email);
            return Result<RegisterDto>.Success();
        }


        public async Task<bool> LogoutAsync()
        {
            return await _identityService.LogoutUserAsync();
        }

        public async Task<bool> SendPasswordResetLinkAsync(string email)
        {
            if (!await _identityService.IsUserExists(email))
            {
                _logger.LogInformation("Password reset link workflow executed.");
                return true;
            }
            EmailMessage emailMessage = new()
            {
                To = email,
                Subject = "Reset Your Password - KeystoneCommerce"
            };
            var result = await _notificationOrchestrator.SendAsync(emailMessage);
            if (!result)
            {
                _logger.LogError("Failed to send password reset link.");
                return true;
            }
            _logger.LogInformation("Password reset link workflow executed.");
            return true;
        }

        public async Task<Result<string>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var result = await _identityService.ResetPasswordAsync(resetPasswordDto.Email, resetPasswordDto.Token, resetPasswordDto.Password);
            if (result.Any())
            {
                _logger.LogWarning("Password reset attempt failed");
                return Result<string>.Failure(result);
            }
            _logger.LogInformation("Password reset workflow completed successfully.");
            return Result<string>.Success();
        }
    }
}
