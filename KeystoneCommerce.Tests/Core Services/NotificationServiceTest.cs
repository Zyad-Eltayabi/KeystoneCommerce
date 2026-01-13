using KeystoneCommerce.Application.Notifications.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("NotificationServiceTests")]
public class NotificationServiceTest
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly NotificationService _sut;

    public NotificationServiceTest()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _sut = new NotificationService(_mockServiceProvider.Object);
    }

    #region SendAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task SendAsync_ShouldReturnTrue_WhenEmailMessageSentSuccessfully()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            NotificationType = NotificationType.OrderConfirmation
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(emailMessage))
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var result = await _sut.SendAsync(emailMessage);

        // Assert
        result.Should().BeTrue();
        mockEmailService.Verify(s => s.SendNotificationAsync(emailMessage), Times.Once);
        _mockServiceProvider.Verify(p => p.GetService(typeof(INotificationService<EmailMessage>)), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldReturnFalse_WhenEmailMessageFailsToSend()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            NotificationType = NotificationType.PasswordReset
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(emailMessage))
            .ReturnsAsync(false);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var result = await _sut.SendAsync(emailMessage);

        // Assert
        result.Should().BeFalse();
        mockEmailService.Verify(s => s.SendNotificationAsync(emailMessage), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldCallGetRequiredService_WithCorrectType()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "user@example.com",
            Subject = "Subject",
            Body = "Body",
            NotificationType = NotificationType.OrderConfirmation
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        await _sut.SendAsync(emailMessage);

        // Assert
        _mockServiceProvider.Verify(
            p => p.GetService(typeof(INotificationService<EmailMessage>)),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldPassCorrectMessage_ToNotificationService()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "specific@example.com",
            Subject = "Specific Subject",
            Body = "Specific Body",
            NotificationType = NotificationType.PasswordReset
        };

        EmailMessage? capturedMessage = null;
        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()))
            .Callback<EmailMessage>(msg => capturedMessage = msg)
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        await _sut.SendAsync(emailMessage);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage.Should().BeEquivalentTo(emailMessage);
        capturedMessage!.To.Should().Be("specific@example.com");
        capturedMessage.Subject.Should().Be("Specific Subject");
        capturedMessage.Body.Should().Be("Specific Body");
        capturedMessage.NotificationType.Should().Be(NotificationType.PasswordReset);
    }

    [Theory]
    [InlineData(NotificationType.OrderConfirmation)]
    [InlineData(NotificationType.PasswordReset)]
    public async Task SendAsync_ShouldHandleDifferentNotificationTypes_Successfully(NotificationType notificationType)
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Body",
            NotificationType = notificationType
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var result = await _sut.SendAsync(emailMessage);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SendAsync_ShouldThrowInvalidOperationException_WhenServiceNotRegistered()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Body",
            NotificationType = NotificationType.OrderConfirmation
        };

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(null);

        // Act
        var act = async () => await _sut.SendAsync(emailMessage);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*INotificationService*EmailMessage*");
    }

    [Fact]
    public async Task SendAsync_ShouldPropagateException_WhenNotificationServiceThrows()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Body",
            NotificationType = NotificationType.OrderConfirmation
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()))
            .ThrowsAsync(new Exception("Email service error"));

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var act = async () => await _sut.SendAsync(emailMessage);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Email service error");
    }

    [Fact]
    public async Task SendAsync_ShouldPropagateException_WhenServiceProviderThrows()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Body",
            NotificationType = NotificationType.OrderConfirmation
        };

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Throws(new InvalidOperationException("Service resolution failed"));

        // Act
        var act = async () => await _sut.SendAsync(emailMessage);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Service resolution failed");
    }

    [Fact]
    public async Task SendAsync_ShouldPropagateException_WhenSmtpServerUnavailable()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Body",
            NotificationType = NotificationType.PasswordReset
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()))
            .ThrowsAsync(new System.Net.Mail.SmtpException("Unable to connect to SMTP server"));

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var act = async () => await _sut.SendAsync(emailMessage);

        // Assert
        await act.Should().ThrowAsync<System.Net.Mail.SmtpException>()
            .WithMessage("*SMTP*");
    }

    [Fact]
    public async Task SendAsync_ShouldPropagateException_WhenAuthenticationFails()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Body",
            NotificationType = NotificationType.OrderConfirmation
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()))
            .ThrowsAsync(new UnauthorizedAccessException("Authentication failed"));

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var act = async () => await _sut.SendAsync(emailMessage);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Authentication failed");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SendAsync_ShouldHandle_EmptyEmailAddress()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "",
            Subject = "Test",
            Body = "Body",
            NotificationType = NotificationType.OrderConfirmation
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(emailMessage))
            .ReturnsAsync(false);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var result = await _sut.SendAsync(emailMessage);

        // Assert
        result.Should().BeFalse();
        mockEmailService.Verify(s => s.SendNotificationAsync(emailMessage), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldHandle_NullSubject()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = null!,
            Body = "Body",
            NotificationType = NotificationType.PasswordReset
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(emailMessage))
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var result = await _sut.SendAsync(emailMessage);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_ShouldHandle_VeryLongEmailBody()
    {
        // Arrange
        var longBody = new string('A', 100000); // 100KB of text
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Long Body Test",
            Body = longBody,
            NotificationType = NotificationType.OrderConfirmation
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(emailMessage))
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var result = await _sut.SendAsync(emailMessage);

        // Assert
        result.Should().BeTrue();
        mockEmailService.Verify(s => s.SendNotificationAsync(
            It.Is<EmailMessage>(m => m.Body.Length == 100000)), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldHandle_SpecialCharactersInEmailContent()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test+special@example.com",
            Subject = "Test <>&\"' Subject",
            Body = "Body with special chars: <script>alert('test')</script>",
            NotificationType = NotificationType.OrderConfirmation
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(emailMessage))
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var result = await _sut.SendAsync(emailMessage);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_ShouldHandle_UnicodeCharactersInMessage()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "测试主题 🎉",
            Body = "مرحبا العالم - Hello World - 你好世界",
            NotificationType = NotificationType.OrderConfirmation
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(emailMessage))
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var result = await _sut.SendAsync(emailMessage);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_ShouldHandle_MultipleConsecutiveCalls()
    {
        // Arrange
        var emailMessage1 = new EmailMessage
        {
            To = "test1@example.com",
            Subject = "Test 1",
            Body = "Body 1",
            NotificationType = NotificationType.OrderConfirmation
        };

        var emailMessage2 = new EmailMessage
        {
            To = "test2@example.com",
            Subject = "Test 2",
            Body = "Body 2",
            NotificationType = NotificationType.PasswordReset
        };

        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        var result1 = await _sut.SendAsync(emailMessage1);
        var result2 = await _sut.SendAsync(emailMessage2);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        mockEmailService.Verify(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()), Times.Exactly(2));
        _mockServiceProvider.Verify(p => p.GetService(typeof(INotificationService<EmailMessage>)), Times.Exactly(2));
    }

    [Fact]
    public async Task SendAsync_ShouldHandle_ConcurrentCalls()
    {
        // Arrange
        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        var messages = Enumerable.Range(1, 10)
            .Select(i => new EmailMessage
            {
                To = $"test{i}@example.com",
                Subject = $"Test {i}",
                Body = $"Body {i}",
                NotificationType = NotificationType.OrderConfirmation
            })
            .ToList();

        // Act
        var tasks = messages.Select(msg => _sut.SendAsync(msg));
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBeEquivalentTo(true);
        mockEmailService.Verify(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()), Times.Exactly(10));
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task SendAsync_ShouldNotModifyMessage_BeforePassingToService()
    {
        // Arrange
        var originalMessage = new EmailMessage
        {
            To = "original@example.com",
            Subject = "Original Subject",
            Body = "Original Body",
            NotificationType = NotificationType.OrderConfirmation
        };

        EmailMessage? receivedMessage = null;
        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()))
            .Callback<EmailMessage>(msg => receivedMessage = msg)
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        await _sut.SendAsync(originalMessage);

        // Assert
        receivedMessage.Should().BeSameAs(originalMessage);
        receivedMessage!.To.Should().Be("original@example.com");
        receivedMessage.Subject.Should().Be("Original Subject");
        receivedMessage.Body.Should().Be("Original Body");
    }

    [Fact]
    public async Task SendAsync_ShouldPreserveAllMessageProperties()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Subject",
            Body = "Body",
            NotificationType = NotificationType.PasswordReset
        };

        EmailMessage? capturedMessage = null;
        var mockEmailService = new Mock<INotificationService<EmailMessage>>();
        mockEmailService.Setup(s => s.SendNotificationAsync(It.IsAny<EmailMessage>()))
            .Callback<EmailMessage>(msg => capturedMessage = msg)
            .ReturnsAsync(true);

        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationService<EmailMessage>)))
            .Returns(mockEmailService.Object);

        // Act
        await _sut.SendAsync(emailMessage);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.To.Should().Be(emailMessage.To);
        capturedMessage.Subject.Should().Be(emailMessage.Subject);
        capturedMessage.Body.Should().Be(emailMessage.Body);
        capturedMessage.NotificationType.Should().Be(emailMessage.NotificationType);
    }

    #endregion

    #endregion

    #region Helper Classes

    private class CustomTestMessage
    {
        public string Content { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    #endregion
}
