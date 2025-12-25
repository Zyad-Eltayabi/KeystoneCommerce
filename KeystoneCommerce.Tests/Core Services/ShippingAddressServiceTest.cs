using KeystoneCommerce.Application.DTOs.ShippingDetails;
using KeystoneCommerce.Infrastructure.Validation.Validators.ShippingAddress;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("ShippingAddressServiceTests")]
public class ShippingAddressServiceTest
{
    private readonly IApplicationValidator<CreateShippingDetailsDto> _validator;
    private readonly IMappingService _mappingService;
    private readonly Mock<IShippingAddressRepository> _mockRepository;
    private readonly Mock<ILogger<ShippingAddressService>> _mockLogger;
    private readonly ShippingAddressService _sut;

    public ShippingAddressServiceTest()
    {
        var fluentValidator = new CreateShippingDetailsDtoValidator();
        _validator = new FluentValidationAdapter<CreateShippingDetailsDto>(fluentValidator);

        _mappingService = new MappingService(MapperHelper.CreateMapper());

        _mockRepository = new Mock<IShippingAddressRepository>();
        _mockLogger = new Mock<ILogger<ShippingAddressService>>();

        _sut = new ShippingAddressService(
            _validator,
            _mockRepository.Object,
            _mappingService,
            _mockLogger.Object);
    }

    #region Happy Path Scenarios

    [Fact]
    public async Task CreateNewAddress_ShouldReturnSuccess_WhenValidInputWithAllRequiredFields()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        SetupSuccessfulRepositoryOperations();

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeGreaterThan(0);
        result.Errors.Should().BeEmpty();

        _mockRepository.Verify(r => r.AddAsync(It.Is<ShippingAddress>(a =>
            a.UserId == dto.UserId &&
            a.Email == dto.Email &&
            a.FullName == dto.FullName &&
            a.Address == dto.Address &&
            a.City == dto.City &&
            a.Country == dto.Country &&
            a.Phone == dto.Phone &&
            a.PostalCode == dto.PostalCode
        )), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnSuccess_WhenValidInputWithOptionalPostalCode()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.PostalCode = "12345";
        SetupSuccessfulRepositoryOperations();

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnSuccess_WhenValidInputWithoutOptionalPostalCode()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.PostalCode = null;
        SetupSuccessfulRepositoryOperations();

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnSuccess_WhenValidInputAtMaximumLengths()
    {
        // Arrange
        var dto = new CreateShippingDetailsDto
        {
            FullName = new string('A', 200),
            Email = new string('a', 244) + "@example.com", // 256 chars total
            Address = new string('B', 500),
            City = new string('C', 100),
            Country = new string('D', 100),
            Phone = new string('1', 20),
            PostalCode = new string('9', 20),
            UserId = "user123"
        };
        SetupSuccessfulRepositoryOperations();

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnSuccess_WhenValidInputAtMinimumLengths()
    {
        // Arrange
        var dto = new CreateShippingDetailsDto
        {
            FullName = "A",
            Email = "a@b.c",
            Address = "1",
            City = "C",
            Country = "D",
            Phone = "1",
            UserId = "u"
        };
        SetupSuccessfulRepositoryOperations();

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("first+last@test-domain.com")]
    public async Task CreateNewAddress_ShouldReturnSuccess_WhenValidEmailFormats(string email)
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.Email = email;
        SetupSuccessfulRepositoryOperations();

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeGreaterThan(0);
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenFullNameIsEmpty()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.FullName = "";

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Full name is required.");
        result.Data.Should().Be(0);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenEmailIsEmpty()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.Email = "";

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Email is required.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenAddressIsEmpty()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.Address = "";

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Address is required.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenCityIsEmpty()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.City = "";

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("City is required.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenCountryIsEmpty()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.Country = "";

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Country is required.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenPhoneIsEmpty()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.Phone = "";

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Phone is required.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test.example.com")]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenEmailFormatIsInvalid(string invalidEmail)
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.Email = invalidEmail;

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email address.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenFullNameExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.FullName = new string('A', 201);

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Full name cannot exceed 200 characters.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenEmailExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.Email = new string('a', 245) + "@example.com"; // 257 chars

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Email cannot exceed 256 characters.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenAddressExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.Address = new string('B', 501);

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Address cannot exceed 500 characters.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenCityExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.City = new string('C', 101);

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("City cannot exceed 100 characters.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenCountryExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.Country = new string('D', 101);

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Country cannot exceed 100 characters.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenPhoneExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.Phone = new string('1', 21);

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Phone cannot exceed 20 characters.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenPostalCodeExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.PostalCode = new string('9', 21);

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Postal code cannot exceed 20 characters.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(TestData.WhiteSpaceTestData), MemberType = typeof(TestData))]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenFullNameIsWhitespace(string whitespace)
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.FullName = whitespace;

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Full name is required.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(TestData.WhiteSpaceTestData), MemberType = typeof(TestData))]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenAddressIsWhitespace(string whitespace)
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.Address = whitespace;

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Address is required.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenMultipleValidationErrorsOccur()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.FullName = "";
        dto.Email = "invalid-email";
        dto.Phone = new string('1', 21);

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain("Full name is required.");
        result.Errors.Should().Contain("Invalid email address.");
        result.Errors.Should().Contain("Phone cannot exceed 20 characters.");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Never);
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task CreateNewAddress_ShouldSetUserId_WhenMappingOccurs()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.UserId = "user123";

        ShippingAddress? capturedAddress = null;

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ShippingAddress>()))
            .Callback<ShippingAddress>(addr => capturedAddress = addr)
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedAddress.Should().NotBeNull();
        capturedAddress!.UserId.Should().Be("user123");
    }

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenSaveChangesReturnsZero()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        _mockRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Failed to create shipping address.");
        result.Data.Should().Be(0);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ShippingAddress>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region Data Integrity Scenarios

    [Fact]
    public async Task CreateNewAddress_ShouldPreserveAllDtoProperties_WhenMappingToEntity()
    {
        // Arrange
        var dto = new CreateShippingDetailsDto
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Address = "123 Main St",
            City = "New York",
            Country = "USA",
            Phone = "+1234567890",
            PostalCode = "12345",
            UserId = "user123"
        };

        ShippingAddress? capturedAddress = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ShippingAddress>()))
            .Callback<ShippingAddress>(addr => capturedAddress = addr)
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.CreateNewAddress(dto);

        // Assert
        capturedAddress.Should().NotBeNull();
        capturedAddress!.FullName.Should().Be(dto.FullName);
        capturedAddress.Email.Should().Be(dto.Email);
        capturedAddress.Address.Should().Be(dto.Address);
        capturedAddress.City.Should().Be(dto.City);
        capturedAddress.Country.Should().Be(dto.Country);
        capturedAddress.Phone.Should().Be(dto.Phone);
        capturedAddress.PostalCode.Should().Be(dto.PostalCode);
        capturedAddress.UserId.Should().Be(dto.UserId);
    }

    [Theory]
    [InlineData("user-1")]
    [InlineData("user-2")]
    [InlineData("admin-123")]
    public async Task CreateNewAddress_ShouldSetCorrectUserId_ForDifferentUsers(string userId)
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.UserId = userId;

        ShippingAddress? capturedAddress = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ShippingAddress>()))
            .Callback<ShippingAddress>(addr => capturedAddress = addr)
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.CreateNewAddress(dto);

        // Assert
        capturedAddress.Should().NotBeNull();
        capturedAddress!.UserId.Should().Be(userId);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CreateNewAddress_ShouldHandleSpecialCharacters_InTextFields()
    {
        // Arrange
        var dto = new CreateShippingDetailsDto
        {
            FullName = "José García-López",
            Email = "jose.garcia@example.com",
            Address = "123 Main St, Apt #456",
            City = "São Paulo",
            Country = "Brasil",
            Phone = "+55-11-98765-4321",
            PostalCode = "01310-100",
            UserId = "user123"
        };
        SetupSuccessfulRepositoryOperations();

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateNewAddress_ShouldHandleEmptyPostalCode_AsOptional()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        dto.PostalCode = "";
        SetupSuccessfulRepositoryOperations();

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public async Task CreateNewAddress_ShouldReturnCorrectId_WhenDifferentIdsAreAssigned(int expectedId)
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();

        ShippingAddress? capturedAddress = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ShippingAddress>()))
            .Callback<ShippingAddress>(addr =>
            {
                addr.Id = expectedId;
                capturedAddress = addr;
            })
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.Data.Should().Be(expectedId);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task CreateNewAddress_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var dto = CreateValidShippingDetailsDto();
        _mockRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        var result = await _sut.CreateNewAddress(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to create shipping address.");
        result.Data.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private static CreateShippingDetailsDto CreateValidShippingDetailsDto()
    {
        return new CreateShippingDetailsDto
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Address = "123 Main Street",
            City = "New York",
            Country = "USA",
            Phone = "+1234567890",
            PostalCode = "12345",
            UserId = "user123"
        };
    }

    private void SetupSuccessfulRepositoryOperations()
    {
        ShippingAddress? capturedAddress = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ShippingAddress>()))
            .Callback<ShippingAddress>(addr =>
            {
                addr.Id = 1; // Simulate database assigning an ID
                capturedAddress = addr;
            })
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);
    }

    #endregion
}