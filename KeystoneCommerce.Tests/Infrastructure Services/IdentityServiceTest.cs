using KeystoneCommerce.Application.DTOs.Order;
using KeystoneCommerce.Infrastructure.Persistence.Identity;
using KeystoneCommerce.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text;

namespace KeystoneCommerce.Tests.Infrastructure_Services;

[Collection("IdentityServiceTests")]
public class IdentityServiceTest
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
    private readonly IdentityService _sut;

    public IdentityServiceTest()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            _mockUserManager.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
            roleStore.Object, null!, null!, null!, null!);

        _sut = new IdentityService(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockRoleManager.Object);
    }

    #region CreateUserAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task CreateUserAsync_ShouldReturnEmptyErrors_WhenUserCreatedSuccessfully()
    {
        // Arrange
        string fullName = "John Doe";
        string email = "john.doe@example.com";
        string password = "Password123!";

        _mockRoleManager.Setup(r => r.RoleExistsAsync(SystemRoles.User))
            .ReturnsAsync(true);
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), SystemRoles.User))
            .ReturnsAsync(IdentityResult.Success);
        _mockSignInManager.Setup(s => s.SignInWithClaimsAsync(
            It.IsAny<ApplicationUser>(), 
            false, 
            It.IsAny<IEnumerable<Claim>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateUserAsync(fullName, email, password);

        // Assert
        result.Should().BeEmpty();
        _mockUserManager.Verify(u => u.CreateAsync(
            It.Is<ApplicationUser>(user => 
                user.Email == email && 
                user.UserName == email && 
                user.FullName == fullName),
            password), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateDefaultRole_WhenRoleDoesNotExist()
    {
        // Arrange
        string fullName = "Jane Doe";
        string email = "jane.doe@example.com";
        string password = "Password123!";

        _mockRoleManager.Setup(r => r.RoleExistsAsync(SystemRoles.User))
            .ReturnsAsync(false);
        _mockRoleManager.Setup(r => r.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), SystemRoles.User))
            .ReturnsAsync(IdentityResult.Success);
        _mockSignInManager.Setup(s => s.SignInWithClaimsAsync(
            It.IsAny<ApplicationUser>(), 
            false, 
            It.IsAny<IEnumerable<Claim>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.CreateUserAsync(fullName, email, password);

        // Assert
        _mockRoleManager.Verify(r => r.CreateAsync(
            It.Is<ApplicationRole>(role => role.Name == SystemRoles.User)), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldSignInUser_AfterSuccessfulCreation()
    {
        // Arrange
        string fullName = "Test User";
        string email = "test@example.com";
        string password = "Password123!";

        _mockRoleManager.Setup(r => r.RoleExistsAsync(SystemRoles.User)).ReturnsAsync(true);
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), SystemRoles.User))
            .ReturnsAsync(IdentityResult.Success);
        _mockSignInManager.Setup(s => s.SignInWithClaimsAsync(
            It.IsAny<ApplicationUser>(), 
            false, 
            It.IsAny<IEnumerable<Claim>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.CreateUserAsync(fullName, email, password);

        // Assert
        _mockSignInManager.Verify(s => s.SignInWithClaimsAsync(
            It.IsAny<ApplicationUser>(),
            false,
            It.Is<IEnumerable<Claim>>(claims => 
                claims.Any(c => c.Type == "FullName" && c.Value == fullName))),
            Times.Once);
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task CreateUserAsync_ShouldReturnErrors_WhenUserCreationFails()
    {
        // Arrange
        string fullName = "Test User";
        string email = "test@example.com";
        string password = "weak";

        _mockRoleManager.Setup(r => r.RoleExistsAsync(SystemRoles.User)).ReturnsAsync(true);
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Password too weak" }));

        // Act
        var result = await _sut.CreateUserAsync(fullName, email, password);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("Password too weak");
        _mockSignInManager.Verify(s => s.SignInWithClaimsAsync(
            It.IsAny<ApplicationUser>(), 
            It.IsAny<bool>(), 
            It.IsAny<IEnumerable<Claim>>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnErrors_WhenRoleAssignmentFails()
    {
        // Arrange
        string fullName = "Test User";
        string email = "test@example.com";
        string password = "Password123!";

        _mockRoleManager.Setup(r => r.RoleExistsAsync(SystemRoles.User)).ReturnsAsync(true);
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), SystemRoles.User))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Role assignment failed" }));
        _mockSignInManager.Setup(s => s.SignInWithClaimsAsync(
            It.IsAny<ApplicationUser>(), 
            false, 
            It.IsAny<IEnumerable<Claim>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateUserAsync(fullName, email, password);

        // Assert
        result.Should().Contain("Role assignment failed");
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnMultipleErrors_WhenMultipleValidationsFail()
    {
        // Arrange
        string fullName = "Test";
        string email = "invalid-email";
        string password = "x";

        _mockRoleManager.Setup(r => r.RoleExistsAsync(SystemRoles.User)).ReturnsAsync(true);
        _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Password too short" },
                new IdentityError { Description = "Invalid email format" }));

        // Act
        var result = await _sut.CreateUserAsync(fullName, email, password);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Password too short");
        result.Should().Contain("Invalid email format");
    }

    #endregion

    #endregion

    #region LoginUserAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task LoginUserAsync_ShouldReturnTrue_WhenCredentialsAreValid()
    {
        // Arrange
        string email = "user@example.com";
        string password = "Password123!";
        bool rememberMe = false;
        var user = new ApplicationUser { Email = email, FullName = "Test User" };

        _mockUserManager.Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync(user);
        _mockUserManager.Setup(u => u.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);
        _mockSignInManager.Setup(s => s.SignInWithClaimsAsync(
            user, 
            false, 
            It.IsAny<IEnumerable<Claim>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.LoginUserAsync(email, password, rememberMe);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LoginUserAsync_ShouldIncludeFullNameClaim_OnSuccessfulLogin()
    {
        // Arrange
        string email = "user@example.com";
        string password = "Password123!";
        var user = new ApplicationUser { Email = email, FullName = "John Smith" };

        _mockUserManager.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.CheckPasswordAsync(user, password)).ReturnsAsync(true);
        _mockSignInManager.Setup(s => s.SignInWithClaimsAsync(
            user, 
            false, 
            It.IsAny<IEnumerable<Claim>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.LoginUserAsync(email, password, false);

        // Assert
        _mockSignInManager.Verify(s => s.SignInWithClaimsAsync(
            user,
            false,
            It.Is<IEnumerable<Claim>>(claims => 
                claims.Any(c => c.Type == "FullName" && c.Value == "John Smith"))),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task LoginUserAsync_ShouldReturnFalse_WhenUserNotFound()
    {
        // Arrange
        string email = "nonexistent@example.com";
        string password = "Password123!";

        _mockUserManager.Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.LoginUserAsync(email, password, false);

        // Assert
        result.Should().BeFalse();
        _mockSignInManager.Verify(s => s.SignInWithClaimsAsync(
            It.IsAny<ApplicationUser>(), 
            It.IsAny<bool>(), 
            It.IsAny<IEnumerable<Claim>>()), Times.Never);
    }

    [Fact]
    public async Task LoginUserAsync_ShouldReturnFalse_WhenPasswordIsIncorrect()
    {
        // Arrange
        string email = "user@example.com";
        string password = "WrongPassword";
        var user = new ApplicationUser { Email = email, FullName = "Test" };

        _mockUserManager.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.CheckPasswordAsync(user, password)).ReturnsAsync(false);

        // Act
        var result = await _sut.LoginUserAsync(email, password, false);

        // Assert
        result.Should().BeFalse();
        _mockSignInManager.Verify(s => s.SignInWithClaimsAsync(
            It.IsAny<ApplicationUser>(), 
            It.IsAny<bool>(), 
            It.IsAny<IEnumerable<Claim>>()), Times.Never);
    }

    #endregion

    #endregion

    #region LogoutUserAsync Tests

    [Fact]
    public async Task LogoutUserAsync_ShouldCallSignOut_AndReturnTrue()
    {
        // Arrange
        _mockSignInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.LogoutUserAsync();

        // Assert
        result.Should().BeTrue();
        _mockSignInManager.Verify(s => s.SignOutAsync(), Times.Once);
    }

    #endregion

    #region IsUserExists Tests

    [Fact]
    public async Task IsUserExists_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        string email = "existing@example.com";
        var user = new ApplicationUser { Email = email };

        _mockUserManager.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(user);

        // Act
        var result = await _sut.IsUserExists(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserExists_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        string email = "nonexistent@example.com";

        _mockUserManager.Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.IsUserExists(email);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ResetPasswordAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnEmptyErrors_WhenPasswordResetSucceeds()
    {
        // Arrange
        string email = "user@example.com";
        string newPassword = "NewPassword123!";
        string rawToken = "reset-token-123";
        string encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var user = new ApplicationUser { Email = email };

        _mockUserManager.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.ResetPasswordAsync(user, rawToken, newPassword))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(u => u.UpdateSecurityStampAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.ResetPasswordAsync(email, encodedToken, newPassword);

        // Assert
        result.Should().BeEmpty();
        _mockUserManager.Verify(u => u.UpdateSecurityStampAsync(user), Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnError_WhenUserNotFound()
    {
        // Arrange
        string email = "nonexistent@example.com";
        string token = "some-token";
        string newPassword = "NewPassword123!";

        _mockUserManager.Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.ResetPasswordAsync(email, token, newPassword);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("Invalid password reset request.");
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnErrors_WhenResetFails()
    {
        // Arrange
        string email = "user@example.com";
        string newPassword = "weak";
        string rawToken = "reset-token";
        string encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var user = new ApplicationUser { Email = email };

        _mockUserManager.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(user);
        _mockUserManager.Setup(u => u.ResetPasswordAsync(user, rawToken, newPassword))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Password does not meet requirements" }));

        // Act
        var result = await _sut.ResetPasswordAsync(email, encodedToken, newPassword);

        // Assert
        result.Should().Contain("Password does not meet requirements");
        _mockUserManager.Verify(u => u.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #endregion
}
