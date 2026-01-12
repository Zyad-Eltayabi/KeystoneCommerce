using FluentValidation;
using KeystoneCommerce.Application.Common.Validation;
using KeystoneCommerce.Application.DTOs.Account;
using KeystoneCommerce.Application.Notifications.Contracts;
using KeystoneCommerce.Infrastructure.Validation;
using KeystoneCommerce.Infrastructure.Validation.Validators.Account;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("AccountServiceTests")]
public class AccountServiceTest
{
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<ILogger<AccountService>> _mockLogger;
    private readonly IApplicationValidator<RegisterDto> _registerValidator;
    private readonly IApplicationValidator<LoginDto> _loginValidator;
    private readonly Mock<INotificationOrchestrator> _mockNotificationOrchestrator;
    private readonly AccountService _sut;

    public AccountServiceTest()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockLogger = new Mock<ILogger<AccountService>>();
        _mockNotificationOrchestrator = new Mock<INotificationOrchestrator>();

        // Use REAL validators instead of mocks
        _registerValidator = new FluentValidationAdapter<RegisterDto>(new RegisterDtoValidator());
        _loginValidator = new FluentValidationAdapter<LoginDto>(new LoginDtoValidator());

        _sut = new AccountService(
            _mockIdentityService.Object,
            _mockLogger.Object,
            _registerValidator,
            _registerValidator,
            _loginValidator,
            _mockNotificationOrchestrator.Object);
    }

    #region RegisterAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task RegisterAsync_ShouldReturnSuccess_WhenValidRegistration()
    {
        // Arrange
        var registerDto = CreateValidRegisterDto();

        _mockIdentityService.Setup(i => i.CreateUserAsync(registerDto.FullName, registerDto.Email, registerDto.Password))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(registerDto);
        result.Errors.Should().BeEmpty();

        _mockIdentityService.Verify(i => i.CreateUserAsync(registerDto.FullName, registerDto.Email, registerDto.Password), Times.Once);
    }

    [Theory]
    [InlineData("ABC", "test@example.com", "Password123!")] // Minimum length full name
    [InlineData("John Doe", "john@example.com", "Password123!")] // Normal case
    [InlineData("Very Long Name That Is Still Valid Within The 100 Character Limit For Testing Purposes Here", "long@test.com", "SecurePass1!")] // Long name
    public async Task RegisterAsync_ShouldHandleValidFullNameLengths_Successfully(string fullName, string email, string password)
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = fullName,
            Email = email,
            Password = password
        };

        _mockIdentityService.Setup(i => i.CreateUserAsync(fullName, email, password))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("Password1!")] // 10 chars - valid
    [InlineData("Pass123!")] // 8 chars - minimum
    [InlineData("VeryLongPassword123!WithManyCharactersThatStillMeetsTheRequirementsForSecurityPurposesHere")] // Long password
    public async Task RegisterAsync_ShouldHandleValidPasswordLengths_Successfully(string password)
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = password
        };

        _mockIdentityService.Setup(i => i.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), password))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("user_123@sub.example.com")]
    public async Task RegisterAsync_ShouldHandleVariousValidEmailFormats_Successfully(string email)
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = email,
            Password = "Password123!"
        };

        _mockIdentityService.Setup(i => i.CreateUserAsync(It.IsAny<string>(), email, It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_ShouldHandleUnicodeCharacters_InFullName()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "José García-López ????",
            Email = "test@example.com",
            Password = "Password123!"
        };

        _mockIdentityService.Setup(i => i.CreateUserAsync(registerDto.FullName, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenFullNameIsEmpty()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "",
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Username is required.");
        result.Data.Should().BeNull();

        _mockIdentityService.Verify(i => i.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(TestData.WhiteSpaceTestData), MemberType = typeof(TestData))]
    public async Task RegisterAsync_ShouldReturnFailure_WhenFullNameIsWhitespace(string fullName)
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = fullName,
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Username is required.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenFullNameIsTooShort()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "AB", // Less than 3 characters
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Username must be at least 3 characters long.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenFullNameExceedsMaxLength()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = new string('A', 101), // 101 characters - exceeds max of 100
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Username must not exceed 100 characters.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenEmailIsEmpty()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = "",
            Password = "Password123!"
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Email is required.");
    }

    [Theory]
    [InlineData("notanemail")] // No @ symbol
    [InlineData("invalid@")] // No domain
    [InlineData("@example.com")] // No local part
    [InlineData("test@")] // Incomplete domain
    public async Task RegisterAsync_ShouldReturnFailure_WhenEmailFormatIsInvalid(string email)
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = email,
            Password = "Password123!"
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email format.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenEmailExceedsMaxLength()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = new string('a', 90) + "@example.com", // 101+ characters
            Password = "Password123!"
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Email must not exceed 100 characters.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenPasswordIsEmpty()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = ""
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Password is required.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenPasswordIsTooShort()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "Pass1!" // Only 6 characters
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Password must be at least 8 characters long.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenPasswordExceedsMaxLength()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "Aa1!" + new string('x', 97) // 101 characters
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Password must not exceed 100 characters.");
    }

    [Theory]
    [InlineData("password123!")] // No uppercase
    [InlineData("PASSWORD123!")] // No lowercase
    [InlineData("Password!@#")] // No digit
    [InlineData("Password123")] // No special character
    [InlineData("Pass1")] // Too short
    public async Task RegisterAsync_ShouldReturnFailure_WhenPasswordDoesNotMeetComplexityRequirements(string password)
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Should have at least one error about password requirements
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnMultipleErrors_WhenMultipleFieldsAreInvalid()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "", // Empty
            Email = "notanemail", // Invalid format
            Password = "weak" // Too short and weak
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1); // Multiple validation errors
        result.Errors.Should().Contain("Username is required.");
        result.Errors.Should().Contain("Invalid email format.");
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenIdentityServiceReturnsErrors()
    {
        // Arrange
        var registerDto = CreateValidRegisterDto();
        var identityErrors = new List<string> { "User with this email already exists." };

        _mockIdentityService.Setup(i => i.CreateUserAsync(registerDto.FullName, registerDto.Email, registerDto.Password))
            .ReturnsAsync(identityErrors);

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("User with this email already exists.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_ShouldNotCallIdentityService_WhenValidationFails()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "A", // Too short
            Email = "invalid",
            Password = "weak"
        };

        // Act
        await _sut.RegisterAsync(registerDto);

        // Assert
        _mockIdentityService.Verify(i => i.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldPassExactParameters_ToIdentityService()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "John Smith",
            Email = "john.smith@example.com",
            Password = "SecurePass123!"
        };

        string? capturedFullName = null;
        string? capturedEmail = null;
        string? capturedPassword = null;

        _mockIdentityService.Setup(i => i.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((fn, em, pw) =>
            {
                capturedFullName = fn;
                capturedEmail = em;
                capturedPassword = pw;
            })
            .ReturnsAsync(new List<string>());

        // Act
        await _sut.RegisterAsync(registerDto);

        // Assert
        capturedFullName.Should().Be("John Smith");
        capturedEmail.Should().Be("john.smith@example.com");
        capturedPassword.Should().Be("SecurePass123!");
    }

    #endregion

    #endregion

    #region LoginAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task LoginAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        // Arrange
        var loginDto = CreateValidLoginDto();

        _mockIdentityService.Setup(i => i.LoginUserAsync(loginDto.Email, loginDto.Password, loginDto.RememberMe))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _mockIdentityService.Verify(i => i.LoginUserAsync(loginDto.Email, loginDto.Password, loginDto.RememberMe), Times.Once);
    }

    [Theory]
    [InlineData(true)] // RememberMe enabled
    [InlineData(false)] // RememberMe disabled
    public async Task LoginAsync_ShouldHandleRememberMeOption_Correctly(bool rememberMe)
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!",
            RememberMe = rememberMe
        };

        _mockIdentityService.Setup(i => i.LoginUserAsync(loginDto.Email, loginDto.Password, rememberMe))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockIdentityService.Verify(i => i.LoginUserAsync(loginDto.Email, loginDto.Password, rememberMe), Times.Once);
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task LoginAsync_ShouldReturnGenericError_WhenEmailIsEmpty()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "",
            Password = "Password123!",
            RememberMe = false
        };

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Security: Returns generic error message instead of specific validation errors
        result.Errors.Should().Contain("Invalid email or password.");

        _mockIdentityService.Verify(i => i.LoginUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    public async Task LoginAsync_ShouldReturnGenericError_WhenEmailFormatIsInvalid(string email)
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = email,
            Password = "Password123!",
            RememberMe = false
        };

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnGenericError_WhenPasswordIsEmpty()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "",
            RememberMe = false
        };

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnGenericError_WhenPasswordIsTooShort()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Pass1!", // Only 6 characters
            RememberMe = false
        };

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password.");
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task LoginAsync_ShouldReturnGenericError_WhenIdentityServiceReturnsFalse()
    {
        // Arrange
        var loginDto = CreateValidLoginDto();

        _mockIdentityService.Setup(i => i.LoginUserAsync(loginDto.Email, loginDto.Password, loginDto.RememberMe))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldNotCallIdentityService_WhenValidationFails()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "invalid",
            Password = "weak",
            RememberMe = false
        };

        // Act
        await _sut.LoginAsync(loginDto);

        // Assert
        _mockIdentityService.Verify(i => i.LoginUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    #endregion

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_ShouldReturnTrue_WhenLogoutSuccessful()
    {
        // Arrange
        _mockIdentityService.Setup(i => i.LogoutUserAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _sut.LogoutAsync();

        // Assert
        result.Should().BeTrue();
        _mockIdentityService.Verify(i => i.LogoutUserAsync(), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnFalse_WhenLogoutFails()
    {
        // Arrange
        _mockIdentityService.Setup(i => i.LogoutUserAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _sut.LogoutAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SendPasswordResetLinkAsync Tests

    #region Security Scenarios

    [Fact]
    public async Task SendPasswordResetLinkAsync_ShouldReturnTrue_AndNotSendEmail_WhenUserDoesNotExist()
    {
        // Arrange - Security: Prevent user enumeration
        string email = "nonexistent@example.com";

        _mockIdentityService.Setup(i => i.IsUserExists(email))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SendPasswordResetLinkAsync(email);

        // Assert
        result.Should().BeTrue(); // Always returns true for security
        _mockIdentityService.Verify(i => i.IsUserExists(email), Times.Once);
        _mockNotificationOrchestrator.Verify(n => n.SendAsync(It.IsAny<EmailMessage>()), Times.Never);
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_ShouldReturnTrue_WhenUserExistsAndEmailSent()
    {
        // Arrange
        string email = "user@example.com";

        _mockIdentityService.Setup(i => i.IsUserExists(email))
            .ReturnsAsync(true);
        _mockNotificationOrchestrator.Setup(n => n.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SendPasswordResetLinkAsync(email);

        // Assert
        result.Should().BeTrue();
        _mockIdentityService.Verify(i => i.IsUserExists(email), Times.Once);
        _mockNotificationOrchestrator.Verify(n => n.SendAsync(It.Is<EmailMessage>(
            em => em.To == email &&
                  em.Subject == "Reset Your Password - KeystoneCommerce" &&
                  em.NotificationType == NotificationType.PasswordReset)),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_ShouldReturnTrue_EvenWhenEmailSendingFails()
    {
        // Arrange - Security: Don't reveal email sending failures
        string email = "user@example.com";

        _mockIdentityService.Setup(i => i.IsUserExists(email))
            .ReturnsAsync(true);
        _mockNotificationOrchestrator.Setup(n => n.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SendPasswordResetLinkAsync(email);

        // Assert
        result.Should().BeTrue(); // Still returns true for security
    }

    #endregion

    #region Edge Cases

    [Theory]
    [MemberData(nameof(TestData.InvalidStrings), MemberType = typeof(TestData))]
    public async Task SendPasswordResetLinkAsync_ShouldHandleEmptyEmail_Gracefully(string email)
    {
        // Arrange
        _mockIdentityService.Setup(i => i.IsUserExists(email))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SendPasswordResetLinkAsync(email);

        // Assert
        result.Should().BeTrue();
        _mockNotificationOrchestrator.Verify(n => n.SendAsync(It.IsAny<EmailMessage>()), Times.Never);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name+tag@example.co.uk")]
    [InlineData("test_email@subdomain.example.com")]
    public async Task SendPasswordResetLinkAsync_ShouldHandleVariousEmailFormats_Correctly(string email)
    {
        // Arrange
        _mockIdentityService.Setup(i => i.IsUserExists(email))
            .ReturnsAsync(true);
        _mockNotificationOrchestrator.Setup(n => n.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SendPasswordResetLinkAsync(email);

        // Assert
        result.Should().BeTrue();
        _mockNotificationOrchestrator.Verify(n => n.SendAsync(It.Is<EmailMessage>(em => em.To == email)), Times.Once);
    }

    #endregion

    #endregion

    #region ResetPasswordAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnSuccess_WhenPasswordResetSuccessful()
    {
        // Arrange
        var resetDto = CreateValidResetPasswordDto();

        _mockIdentityService.Setup(i => i.ResetPasswordAsync(resetDto.Email, resetDto.Token, resetDto.Password))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.ResetPasswordAsync(resetDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _mockIdentityService.Verify(i => i.ResetPasswordAsync(resetDto.Email, resetDto.Token, resetDto.Password), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldPassCorrectParameters_ToIdentityService()
    {
        // Arrange
        var resetDto = new ResetPasswordDto
        {
            Email = "test@example.com",
            Token = "reset-token-12345",
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        string? capturedEmail = null;
        string? capturedToken = null;
        string? capturedPassword = null;

        _mockIdentityService.Setup(i => i.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((email, token, password) =>
            {
                capturedEmail = email;
                capturedToken = token;
                capturedPassword = password;
            })
            .ReturnsAsync(new List<string>());

        // Act
        await _sut.ResetPasswordAsync(resetDto);

        // Assert
        capturedEmail.Should().Be("test@example.com");
        capturedToken.Should().Be("reset-token-12345");
        capturedPassword.Should().Be("NewPassword123!");
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnFailure_WhenTokenIsInvalid()
    {
        // Arrange
        var resetDto = CreateValidResetPasswordDto();
        var errors = new List<string> { "Invalid or expired token." };

        _mockIdentityService.Setup(i => i.ResetPasswordAsync(resetDto.Email, resetDto.Token, resetDto.Password))
            .ReturnsAsync(errors);

        // Act
        var result = await _sut.ResetPasswordAsync(resetDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Invalid or expired token.");
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnMultipleErrors_FromIdentityService()
    {
        // Arrange
        var resetDto = CreateValidResetPasswordDto();
        var errors = new List<string>
        {
            "Token has expired.",
            "Password doesn't meet requirements.",
            "User not found."
        };

        _mockIdentityService.Setup(i => i.ResetPasswordAsync(resetDto.Email, resetDto.Token, resetDto.Password))
            .ReturnsAsync(errors);

        // Act
        var result = await _sut.ResetPasswordAsync(resetDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var resetDto = CreateValidResetPasswordDto();
        var errors = new List<string> { "Invalid password reset request." };

        _mockIdentityService.Setup(i => i.ResetPasswordAsync(resetDto.Email, resetDto.Token, resetDto.Password))
            .ReturnsAsync(errors);

        // Act
        var result = await _sut.ResetPasswordAsync(resetDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid password reset request.");
    }

    #endregion

    #endregion

    #region Helper Methods

    private static RegisterDto CreateValidRegisterDto()
    {
        return new RegisterDto
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Password = "Password123!"
        };
    }

    private static LoginDto CreateValidLoginDto()
    {
        return new LoginDto
        {
            Email = "john.doe@example.com",
            Password = "Password123!",
            RememberMe = false
        };
    }

    private static ResetPasswordDto CreateValidResetPasswordDto()
    {
        return new ResetPasswordDto
        {
            Email = "john.doe@example.com",
            Token = "valid-reset-token",
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };
    }

    #endregion
}
