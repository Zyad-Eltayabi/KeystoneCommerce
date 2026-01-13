using KeystoneCommerce.Application.Notifications.Contracts;
using KeystoneCommerce.Infrastructure.Helpers;
using KeystoneCommerce.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace KeystoneCommerce.Tests.Infrastructure_Services;

[Collection("EmailServiceTests")]
public class EmailServiceTest
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly EmailSettings _emailSettings;
    private readonly EmailService _sut;

    public EmailServiceTest()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["AppSettings:BaseUrl"]).Returns("https://example.com");

        _emailSettings = new EmailSettings
        {
            Host = "smtp.test.com",
            Port = 587,
            EnableSsl = true,
            From = "noreply@test.com"
        };

        var mockOptions = Options.Create(_emailSettings);

        _sut = new EmailService(_mockUserManager.Object, _mockConfiguration.Object, mockOptions);
    }

    #region SendNotificationAsync Tests - Password Reset

    #region Happy Path Scenarios

    [Fact]
    public async Task SendNotificationAsync_PasswordReset_ShouldReturnFalse_WhenUserNotFound()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "nonexistent@example.com",
            Subject = "Password Reset",
            Body = "",
            NotificationType = NotificationType.PasswordReset
        };

        _mockUserManager.Setup(u => u.FindByEmailAsync(message.To))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.SendNotificationAsync(message);

        // Assert
        result.Should().BeFalse();
        _mockUserManager.Verify(u => u.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task SendNotificationAsync_PasswordReset_ShouldGenerateToken_WhenUserExists()
    {
        // Arrange - Note: This will fail at SMTP level but we can verify token generation
        var user = new ApplicationUser { Email = "user@example.com", FullName = "Test User" };
        var message = new EmailMessage
        {
            To = user.Email,
            Subject = "Password Reset",
            Body = "",
            NotificationType = NotificationType.PasswordReset
        };

        _mockUserManager.Setup(u => u.FindByEmailAsync(message.To)).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("test-reset-token");

        // Act - Will return false due to SMTP failure, but we verify the flow
        await _sut.SendNotificationAsync(message);

        // Assert
        _mockUserManager.Verify(u => u.GeneratePasswordResetTokenAsync(user), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SendNotificationAsync_PasswordReset_ShouldHandleEmptyEmail()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "",
            Subject = "Password Reset",
            Body = "",
            NotificationType = NotificationType.PasswordReset
        };

        _mockUserManager.Setup(u => u.FindByEmailAsync(""))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.SendNotificationAsync(message);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendNotificationAsync_PasswordReset_ShouldUseBaseUrlFromConfiguration()
    {
        // Arrange
        var user = new ApplicationUser { Email = "user@example.com", FullName = "Test User" };
        var message = new EmailMessage
        {
            To = user.Email,
            Subject = "Password Reset",
            Body = "",
            NotificationType = NotificationType.PasswordReset
        };

        _mockUserManager.Setup(u => u.FindByEmailAsync(message.To)).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("token");

        // Act
        await _sut.SendNotificationAsync(message);

        // Assert
        _mockConfiguration.Verify(c => c["AppSettings:BaseUrl"], Times.Once);
    }

    #endregion

    #endregion

    #region SendNotificationAsync Tests - Order Confirmation

    #region Happy Path Scenarios

    [Fact]
    public async Task SendNotificationAsync_OrderConfirmation_ShouldReturnFalse_WhenUserNotFound()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "nonexistent-user-id",
            Subject = "Order Confirmation",
            Body = "ORD-123456",
            NotificationType = NotificationType.OrderConfirmation
        };

        _mockUserManager.Setup(u => u.FindByIdAsync(message.To))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.SendNotificationAsync(message);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendNotificationAsync_OrderConfirmation_ShouldLookupUserById()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "user@example.com", FullName = "Test User" };
        var message = new EmailMessage
        {
            To = userId,
            Subject = "Order Confirmation",
            Body = "ORD-123456",
            NotificationType = NotificationType.OrderConfirmation
        };

        _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act
        await _sut.SendNotificationAsync(message);

        // Assert
        _mockUserManager.Verify(u => u.FindByIdAsync(userId), Times.Once);
        _mockUserManager.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #endregion

    #region SendNotificationAsync Tests - Unknown Type

    [Fact]
    public async Task SendNotificationAsync_ShouldReturnFalse_WhenNotificationTypeIsUnknown()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "user@example.com",
            Subject = "Unknown",
            Body = "Test",
            NotificationType = (NotificationType)999 // Unknown type
        };

        // Act
        var result = await _sut.SendNotificationAsync(message);

        // Assert
        result.Should().BeFalse();
        _mockUserManager.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _mockUserManager.Verify(u => u.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Notification Type Routing Tests

    [Fact]
    public async Task SendNotificationAsync_ShouldRoutePasswordReset_ToCorrectHandler()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "user@example.com",
            Subject = "Password Reset",
            Body = "",
            NotificationType = NotificationType.PasswordReset
        };

        _mockUserManager.Setup(u => u.FindByEmailAsync(message.To))
            .ReturnsAsync(new ApplicationUser { Email = message.To, FullName = "Test" });
        _mockUserManager.Setup(u => u.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("token");

        // Act
        await _sut.SendNotificationAsync(message);

        // Assert - Verify password reset specific method was called
        _mockUserManager.Verify(u => u.FindByEmailAsync(message.To), Times.Once);
        _mockUserManager.Verify(u => u.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_ShouldRouteOrderConfirmation_ToCorrectHandler()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "user-id-123",
            Subject = "Order Confirmation",
            Body = "ORD-123",
            NotificationType = NotificationType.OrderConfirmation
        };

        _mockUserManager.Setup(u => u.FindByIdAsync(message.To))
            .ReturnsAsync(new ApplicationUser { Id = message.To, Email = "test@example.com", FullName = "Test" });

        // Act
        await _sut.SendNotificationAsync(message);

        // Assert - Verify order confirmation specific method was called
        _mockUserManager.Verify(u => u.FindByIdAsync(message.To), Times.Once);
        // For order confirmation, no token generation
        _mockUserManager.Verify(u => u.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region Data Integrity Tests

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name+tag@example.com")]
    [InlineData("user@subdomain.example.com")]
    public async Task SendNotificationAsync_PasswordReset_ShouldHandleVariousEmailFormats(string email)
    {
        // Arrange
        var message = new EmailMessage
        {
            To = email,
            Subject = "Password Reset",
            Body = "",
            NotificationType = NotificationType.PasswordReset
        };

        _mockUserManager.Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.SendNotificationAsync(message);

        // Assert
        result.Should().BeFalse();
        _mockUserManager.Verify(u => u.FindByEmailAsync(email), Times.Once);
    }

    [Theory]
    [InlineData("ORD-123456")]
    [InlineData("ORD-ABCDEF")]
    [InlineData("ORDER-2024-001")]
    public async Task SendNotificationAsync_OrderConfirmation_ShouldHandleVariousOrderNumbers(string orderNumber)
    {
        // Arrange
        var userId = "user-123";
        var message = new EmailMessage
        {
            To = userId,
            Subject = "Order Confirmation",
            Body = orderNumber,
            NotificationType = NotificationType.OrderConfirmation
        };

        _mockUserManager.Setup(u => u.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.SendNotificationAsync(message);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
