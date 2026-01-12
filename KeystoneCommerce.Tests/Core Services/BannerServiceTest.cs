using KeystoneCommerce.Application.DTOs;
using KeystoneCommerce.Application.DTOs.Banner;
using KeystoneCommerce.Application.Common.Validation;
using KeystoneCommerce.Infrastructure.Validation.Validators.Banner;
using FluentValidation;
using Xunit;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("BannerServiceTests")]
public class BannerServiceTest
{
    private readonly Mock<IImageService> _mockImageService;
    private readonly Mock<IBannerRepository> _mockBannerRepository;
    private readonly IMappingService _mappingService;
    private readonly IApplicationValidator<CreateBannerDto> _createValidator;
    private readonly IApplicationValidator<UpdateBannerDto> _updateValidator;
    private readonly Mock<ILogger<BannerService>> _mockLogger;
    private readonly BannerService _sut;

    public BannerServiceTest()
    {
        _mockImageService = new Mock<IImageService>();
        _mockBannerRepository = new Mock<IBannerRepository>();
        _mappingService = new MappingService(MapperHelper.CreateMapper());
        _createValidator = new FluentValidationAdapter<CreateBannerDto>(new CreateBannerDtoValidator());
        _updateValidator = new FluentValidationAdapter<UpdateBannerDto>(new UpdateBannerDtoValidator());
        _mockLogger = new Mock<ILogger<BannerService>>();

        _sut = new BannerService(
            _mockImageService.Object,
            _mockBannerRepository.Object,
            _mappingService,
            _createValidator,
            _updateValidator,
            _mockLogger.Object);
    }

    #region Create Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task Create_ShouldReturnSuccess_WhenValidInputProvided()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("banner-123.jpg");

        _mockBannerRepository.Setup(r => r.AddAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _mockImageService.Verify(s => s.SaveImageAsync(
            createDto.Image, createDto.ImageType, createDto.ImageUrl), Times.Once);
        _mockBannerRepository.Verify(r => r.AddAsync(It.IsAny<Banner>()), Times.Once);
        _mockBannerRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(BannerType.HomePage, 1)]
    [InlineData(BannerType.Featured, 2)]
    [InlineData(BannerType.TopProducts, 3)]
    public async Task Create_ShouldReturnSuccess_WithDifferentBannerTypes(BannerType bannerType, int priority)
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.BannerType = (int)bannerType;
        createDto.Priority = priority;

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("banner-image.jpg");

        _mockBannerRepository.Setup(r => r.AddAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldSetImageNameCorrectly_OnSuccess()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        var expectedImageName = "saved-banner-image.jpg";
        Banner? capturedBanner = null;

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedImageName);

        _mockBannerRepository.Setup(r => r.AddAsync(It.IsAny<Banner>()))
            .Callback<Banner>(b => capturedBanner = b)
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedBanner.Should().NotBeNull();
        capturedBanner!.ImageName.Should().Be(expectedImageName);
    }

    [Fact]
    public async Task Create_ShouldMapPropertiesCorrectly()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        Banner? capturedBanner = null;

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        _mockBannerRepository.Setup(r => r.AddAsync(It.IsAny<Banner>()))
            .Callback<Banner>(b => capturedBanner = b)
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _sut.Create(createDto);

        // Assert
        capturedBanner.Should().NotBeNull();
        capturedBanner!.Title.Should().Be(createDto.Title);
        capturedBanner.SubTitle.Should().Be(createDto.Subtitle);
        capturedBanner.Link.Should().Be(createDto.Link);
        capturedBanner.Priority.Should().Be(createDto.Priority);
        capturedBanner.BannerType.Should().Be((BannerType)createDto.BannerType);
    }

    [Fact]
    public async Task Create_ShouldHandleMaximumLengthValues()
    {
        // Arrange
        var createDto = new CreateBannerDto
        {
            Title = new string('A', 200), // Max length
            Subtitle = new string('B', 500), // Max length
            Link = new string('C', 100), // Max length
            Priority = int.MaxValue,
            BannerType = (int)BannerType.HomePage,
            Image = new byte[] { 1, 2, 3 },
            ImageUrl = "/banners",
            ImageType = ".jpg"
        };

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        _mockBannerRepository.Setup(r => r.AddAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldHandleMinimumValidValues()
    {
        // Arrange
        var createDto = new CreateBannerDto
        {
            Title = "A",
            Subtitle = "B",
            Link = "C",
            Priority = 1, // Minimum valid priority
            BannerType = (int)BannerType.HomePage,
            Image = new byte[] { 1 },
            ImageUrl = "/",
            ImageType = ".jpg"
        };

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        _mockBannerRepository.Setup(r => r.AddAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenTitleIsEmpty()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Title = "";

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Title is required.");
        _mockImageService.Verify(s => s.SaveImageAsync(
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockBannerRepository.Verify(r => r.AddAsync(It.IsAny<Banner>()), Times.Never);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenSubtitleIsEmpty()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Subtitle = "";

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Subtitle is required.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenLinkIsEmpty()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Link = "";

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Link is required.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenTitleExceedsMaxLength()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Title = new string('A', 201); // Exceeds max 200

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Title must not exceed 200 characters.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenSubtitleExceedsMaxLength()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Subtitle = new string('B', 501); // Exceeds max 500

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Subtitle must not exceed 500 characters.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenLinkExceedsMaxLength()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Link = new string('C', 101); // Exceeds max 100

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Link must not exceed 100 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Create_ShouldReturnFailure_WhenPriorityIsInvalid(int priority)
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Priority = priority;

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Priority must be a positive integer.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(999)]
    public async Task Create_ShouldReturnFailure_WhenBannerTypeIsInvalid(int bannerType)
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.BannerType = bannerType;

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid Banner Type.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenImageIsNull()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Image = null!;

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Image is required and must not be empty.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenImageIsEmpty()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Image = Array.Empty<byte>();

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Image is required and must not be empty.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenImageTypeIsEmpty()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.ImageType = "";

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid image type.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenImageUrlIsEmpty()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.ImageUrl = "";

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid image.");
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WithMultipleValidationErrors()
    {
        // Arrange
        var createDto = new CreateBannerDto
        {
            Title = "", // Empty
            Subtitle = new string('B', 501), // Too long
            Link = "", // Empty
            Priority = -1, // Invalid
            BannerType = 999, // Invalid
            Image = Array.Empty<byte>(), // Empty
            ImageUrl = "",
            ImageType = ""
        };

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
    }

    [Theory]
    [MemberData(nameof(TestData.WhiteSpaceTestData
        ), MemberType = typeof(TestData))]
    public async Task Create_ShouldReturnFailure_WhenTitleIsWhitespace(string title)
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Title = title;

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Title is required.");
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task Create_ShouldCallImageServiceWithCorrectParameters()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        _mockBannerRepository.Setup(r => r.AddAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _sut.Create(createDto);

        // Assert
        _mockImageService.Verify(s => s.SaveImageAsync(
            createDto.Image,
            createDto.ImageType,
            createDto.ImageUrl), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldCallRepositoryInCorrectOrder()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        var callOrder = new List<string>();

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => callOrder.Add("SaveImage"))
            .ReturnsAsync("image.jpg");

        _mockBannerRepository.Setup(r => r.AddAsync(It.IsAny<Banner>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .Callback(() => callOrder.Add("SaveChanges"))
            .ReturnsAsync(1);

        // Act
        await _sut.Create(createDto);

        // Assert
        callOrder.Should().Equal("SaveImage", "AddAsync", "SaveChanges");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Create_ShouldHandleSpecialCharactersInTitle()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Title = "Banner <>&\"'!@#$%";

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        _mockBannerRepository.Setup(r => r.AddAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldHandleUnicodeCharacters()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Title = "横幅 Banner العربية";

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("image.jpg");

        _mockBannerRepository.Setup(r => r.AddAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldHandleLargeImageData()
    {
        // Arrange
        var createDto = CreateValidCreateBannerDto();
        createDto.Image = new byte[5_000_000]; // 5MB

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("large-image.jpg");

        _mockBannerRepository.Setup(r => r.AddAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Create(createDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #endregion

    #region GetBannerTypes Tests

    [Fact]
    public void GetBannerTypes_ShouldReturnAllBannerTypes()
    {
        // Act
        var result = _sut.GetBannerTypes();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().ContainKey(1).WhoseValue.Should().Be("HomePage");
        result.Should().ContainKey(2).WhoseValue.Should().Be("Featured");
        result.Should().ContainKey(3).WhoseValue.Should().Be("TopProducts");
    }

    [Fact]
    public void GetBannerTypes_ShouldReturnCorrectKeyValuePairs()
    {
        // Act
        var result = _sut.GetBannerTypes();

        // Assert
        result[(int)BannerType.HomePage].Should().Be("HomePage");
        result[(int)BannerType.Featured].Should().Be("Featured");
        result[(int)BannerType.TopProducts].Should().Be("TopProducts");
    }

    #endregion

    #region GetBanners Tests

    [Fact]
    public async Task GetBanners_ShouldReturnAllBanners()
    {
        // Arrange
        var banners = new List<Banner>
        {
            CreateBanner(1, "Banner 1"),
            CreateBanner(2, "Banner 2"),
            CreateBanner(3, "Banner 3")
        };

        _mockBannerRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(banners);

        // Act
        var result = await _sut.GetBanners();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        _mockBannerRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetBanners_ShouldReturnEmptyList_WhenNoBannersExist()
    {
        // Arrange
        _mockBannerRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Banner>());

        // Act
        var result = await _sut.GetBanners();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBanners_ShouldMapPropertiesCorrectly()
    {
        // Arrange
        var banner = CreateBanner(1, "Test Banner");
        _mockBannerRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Banner> { banner });

        // Act
        var result = await _sut.GetBanners();

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.Id.Should().Be(banner.Id);
        dto.Title.Should().Be(banner.Title);
        dto.SubTitle.Should().Be(banner.SubTitle);
        dto.Link.Should().Be(banner.Link);
        dto.Priority.Should().Be(banner.Priority);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ShouldReturnBanner_WhenBannerExists()
    {
        // Arrange
        var banner = CreateBanner(1, "Test Banner");

        _mockBannerRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(banner);

        // Act
        var result = await _sut.GetById(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Title.Should().Be("Test Banner");
        _mockBannerRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenBannerDoesNotExist()
    {
        // Arrange
        _mockBannerRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Banner?)null);

        // Act
        var result = await _sut.GetById(999);

        // Assert
        result.Should().BeNull();
        _mockBannerRepository.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(9999)]
    public async Task GetById_ShouldHandleDifferentIds(int id)
    {
        // Arrange
        var banner = CreateBanner(id, $"Banner {id}");
        _mockBannerRepository.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(banner);

        // Act
        var result = await _sut.GetById(id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetById_ShouldMapAllProperties()
    {
        // Arrange
        var banner = new Banner
        {
            Id = 5,
            Title = "Test Title",
            SubTitle = "Test Subtitle",
            ImageName = "test.jpg",
            Link = "/test",
            Priority = 10,
            BannerType = BannerType.Featured
        };

        _mockBannerRepository.Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(banner);

        // Act
        var result = await _sut.GetById(5);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be(banner.Title);
        result.SubTitle.Should().Be(banner.SubTitle);
        result.ImageName.Should().Be(banner.ImageName);
        result.Link.Should().Be(banner.Link);
        result.Priority.Should().Be(banner.Priority);
    }

    #endregion

    #region UpdateBannerAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task UpdateBannerAsync_ShouldReturnSuccess_WhenValidInputWithoutNewImage()
    {
        // Arrange
        var updateDto = CreateValidUpdateBannerDto();
        updateDto.Image = Array.Empty<byte>(); // No new image
        var existingBanner = CreateBanner(1, "Old Title");

        _mockBannerRepository.Setup(r => r.GetByIdAsync(updateDto.Id))
            .ReturnsAsync(existingBanner);

        _mockBannerRepository.Setup(r => r.Update(It.IsAny<Banner>()));

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateBannerAsync(updateDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _mockImageService.Verify(s => s.SaveImageAsync(
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockImageService.Verify(s => s.DeleteImageAsync(
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockBannerRepository.Verify(r => r.Update(It.IsAny<Banner>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBannerAsync_ShouldReturnSuccess_WhenValidInputWithNewImage()
    {
        // Arrange
        var updateDto = CreateValidUpdateBannerDto();
        updateDto.Image = new byte[] { 1, 2, 3 }; // New image
        var existingBanner = CreateBanner(1, "Old Title");
        var oldImageName = existingBanner.ImageName;

        _mockBannerRepository.Setup(r => r.GetByIdAsync(updateDto.Id))
            .ReturnsAsync(existingBanner);

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("new-image.jpg");

        _mockImageService.Setup(s => s.DeleteImageAsync(
                It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.Update(It.IsAny<Banner>()));

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateBannerAsync(updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockImageService.Verify(s => s.SaveImageAsync(
            updateDto.Image, updateDto.ImageType, updateDto.ImageUrl), Times.Once);
        _mockImageService.Verify(s => s.DeleteImageAsync(
            updateDto.ImageUrl, oldImageName), Times.Once);
    }

    [Fact]
    public async Task UpdateBannerAsync_ShouldUpdateImageName_WhenNewImageProvided()
    {
        // Arrange
        var updateDto = CreateValidUpdateBannerDto();
        updateDto.Image = new byte[] { 1, 2, 3 };
        var existingBanner = CreateBanner(1, "Title");
        var newImageName = "updated-image.jpg";

        _mockBannerRepository.Setup(r => r.GetByIdAsync(updateDto.Id))
            .ReturnsAsync(existingBanner);

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(newImageName);

        _mockImageService.Setup(s => s.DeleteImageAsync(
                It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.Update(It.IsAny<Banner>()));

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _sut.UpdateBannerAsync(updateDto);

        // Assert
        existingBanner.ImageName.Should().Be(newImageName);
    }

    [Fact]
    public async Task UpdateBannerAsync_ShouldNotDeleteOldImage_WhenNoNewImageProvided()
    {
        // Arrange
        var updateDto = CreateValidUpdateBannerDto();
        updateDto.Image = Array.Empty<byte>();
        var existingBanner = CreateBanner(1, "Title");

        _mockBannerRepository.Setup(r => r.GetByIdAsync(updateDto.Id))
            .ReturnsAsync(existingBanner);

        _mockBannerRepository.Setup(r => r.Update(It.IsAny<Banner>()));

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _sut.UpdateBannerAsync(updateDto);

        // Assert
        _mockImageService.Verify(s => s.DeleteImageAsync(
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task UpdateBannerAsync_ShouldReturnFailure_WhenIdIsInvalid()
    {
        // Arrange
        var updateDto = CreateValidUpdateBannerDto();
        updateDto.Id = 0;

        // Act
        var result = await _sut.UpdateBannerAsync(updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid Banner Id.");
        _mockBannerRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateBannerAsync_ShouldReturnFailure_WhenTitleIsEmpty()
    {
        // Arrange
        var updateDto = CreateValidUpdateBannerDto();
        updateDto.Title = "";

        // Act
        var result = await _sut.UpdateBannerAsync(updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Title is required.");
    }

    [Fact]
    public async Task UpdateBannerAsync_ShouldReturnFailure_WhenBannerNotFound()
    {
        // Arrange
        var updateDto = CreateValidUpdateBannerDto();

        _mockBannerRepository.Setup(r => r.GetByIdAsync(updateDto.Id))
            .ReturnsAsync((Banner?)null);

        // Act
        var result = await _sut.UpdateBannerAsync(updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Banner not found!");
        _mockBannerRepository.Verify(r => r.Update(It.IsAny<Banner>()), Times.Never);
    }

    [Fact]
    public async Task UpdateBannerAsync_ShouldReturnFailure_WhenImageTypeIsMissingWithNewImage()
    {
        // Arrange
        var updateDto = CreateValidUpdateBannerDto();
        updateDto.Image = new byte[] { 1, 2, 3 };
        updateDto.ImageType = "";

        // Act
        var result = await _sut.UpdateBannerAsync(updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid image type.");
    }

    [Fact]
    public async Task UpdateBannerAsync_ShouldReturnFailure_WhenImageUrlIsMissingWithNewImage()
    {
        // Arrange
        var updateDto = CreateValidUpdateBannerDto();
        updateDto.Image = new byte[] { 1, 2, 3 };
        updateDto.ImageUrl = "";

        // Act
        var result = await _sut.UpdateBannerAsync(updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid image.");
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task UpdateBannerAsync_ShouldValidateBeforeCheckingExistence()
    {
        // Arrange
        var updateDto = CreateValidUpdateBannerDto();
        updateDto.Title = ""; // Invalid

        // Act
        var result = await _sut.UpdateBannerAsync(updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _mockBannerRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateBannerAsync_ShouldDeleteOldImage_OnlyWhenImageChanges()
    {
        // Arrange
        var updateDto = CreateValidUpdateBannerDto();
        updateDto.Image = new byte[] { 1, 2, 3 };
        var existingBanner = CreateBanner(1, "Title");
        var oldImageName = existingBanner.ImageName;

        _mockBannerRepository.Setup(r => r.GetByIdAsync(updateDto.Id))
            .ReturnsAsync(existingBanner);

        _mockImageService.Setup(s => s.SaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("new-image.jpg");

        _mockImageService.Setup(s => s.DeleteImageAsync(
                It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockBannerRepository.Setup(r => r.Update(It.IsAny<Banner>()));

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _sut.UpdateBannerAsync(updateDto);

        // Assert
        _mockImageService.Verify(s => s.DeleteImageAsync(
            updateDto.ImageUrl, oldImageName), Times.Once);
    }

    #endregion

    #endregion

    #region DeleteBannerAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task DeleteBannerAsync_ShouldReturnSuccess_WhenBannerExists()
    {
        // Arrange
        var bannerId = 1;
        var imageUrl = "/banners";
        var banner = CreateBanner(bannerId, "Test Banner");

        _mockBannerRepository.Setup(r => r.GetByIdAsync(bannerId))
            .ReturnsAsync(banner);

        _mockBannerRepository.Setup(r => r.Delete(It.IsAny<Banner>()));

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockImageService.Setup(s => s.DeleteImageAsync(
                It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteBannerAsync(bannerId, imageUrl);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _mockBannerRepository.Verify(r => r.Delete(banner), Times.Once);
        _mockBannerRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        _mockImageService.Verify(s => s.DeleteImageAsync(imageUrl, banner.ImageName), Times.Once);
    }

    [Fact]
    public async Task DeleteBannerAsync_ShouldDeleteImageAfterDatabaseDeletion()
    {
        // Arrange
        var bannerId = 1;
        var imageUrl = "/banners";
        var banner = CreateBanner(bannerId, "Test");
        var callOrder = new List<string>();

        _mockBannerRepository.Setup(r => r.GetByIdAsync(bannerId))
            .ReturnsAsync(banner);

        _mockBannerRepository.Setup(r => r.Delete(It.IsAny<Banner>()))
            .Callback(() => callOrder.Add("Delete"));

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .Callback(() => callOrder.Add("SaveChanges"))
            .ReturnsAsync(1);

        _mockImageService.Setup(s => s.DeleteImageAsync(
                It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => callOrder.Add("DeleteImage"))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteBannerAsync(bannerId, imageUrl);

        // Assert
        callOrder.Should().Equal("Delete", "SaveChanges", "DeleteImage");
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task DeleteBannerAsync_ShouldReturnFailure_WhenBannerNotFound()
    {
        // Arrange
        var bannerId = 999;
        var imageUrl = "/banners";

        _mockBannerRepository.Setup(r => r.GetByIdAsync(bannerId))
            .ReturnsAsync((Banner?)null);

        // Act
        var result = await _sut.DeleteBannerAsync(bannerId, imageUrl);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Banner not found!");
        _mockBannerRepository.Verify(r => r.Delete(It.IsAny<Banner>()), Times.Never);
        _mockImageService.Verify(s => s.DeleteImageAsync(
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteBannerAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var bannerId = 1;
        var imageUrl = "/banners";
        var banner = CreateBanner(bannerId, "Test");

        _mockBannerRepository.Setup(r => r.GetByIdAsync(bannerId))
            .ReturnsAsync(banner);

        _mockBannerRepository.Setup(r => r.Delete(It.IsAny<Banner>()));

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(0); // Failed to save

        // Act
        var result = await _sut.DeleteBannerAsync(bannerId, imageUrl);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("failed to delete banner");
        _mockImageService.Verify(s => s.DeleteImageAsync(
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteBannerAsync_ShouldNotDeleteImage_WhenDatabaseDeletionFails()
    {
        // Arrange
        var bannerId = 1;
        var imageUrl = "/banners";
        var banner = CreateBanner(bannerId, "Test");

        _mockBannerRepository.Setup(r => r.GetByIdAsync(bannerId))
            .ReturnsAsync(banner);

        _mockBannerRepository.Setup(r => r.Delete(It.IsAny<Banner>()));

        _mockBannerRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(0);

        // Act
        await _sut.DeleteBannerAsync(bannerId, imageUrl);

        // Assert
        _mockImageService.Verify(s => s.DeleteImageAsync(
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #endregion

    #region PrepareBannersForHomePage Tests

    [Fact]
    public async Task PrepareBannersForHomePage_ShouldReturnAllBannerTypes()
    {
        // Arrange
        var homePageBanners = new List<Banner>
        {
            CreateBannerWithType(1, BannerType.HomePage, 1),
            CreateBannerWithType(2, BannerType.HomePage, 2)
        };

        var featuredBanners = new List<Banner>
        {
            CreateBannerWithType(3, BannerType.Featured, 1)
        };

        var topProductsBanners = new List<Banner>
        {
            CreateBannerWithType(4, BannerType.TopProducts, 1),
            CreateBannerWithType(5, BannerType.TopProducts, 2)
        };

        _mockBannerRepository.Setup(r => r.FindAllAsync(
                It.Is<Expression<Func<Banner, bool>>>(expr => 
                    TestBannerTypeExpression(expr, BannerType.HomePage))))
            .ReturnsAsync(homePageBanners);

        _mockBannerRepository.Setup(r => r.FindAllAsync(
                It.Is<Expression<Func<Banner, bool>>>(expr => 
                    TestBannerTypeExpression(expr, BannerType.Featured))))
            .ReturnsAsync(featuredBanners);

        _mockBannerRepository.Setup(r => r.FindAllAsync(
                It.Is<Expression<Func<Banner, bool>>>(expr => 
                    TestBannerTypeExpression(expr, BannerType.TopProducts))))
            .ReturnsAsync(topProductsBanners);

        // Act
        var result = await _sut.PrepareBannersForHomePage();

        // Assert
        result.Should().NotBeNull();
        result.HomePage.Should().HaveCount(2);
        result.Featured.Should().HaveCount(1);
        result.TopProducts.Should().HaveCount(2);
    }

    [Fact]
    public async Task PrepareBannersForHomePage_ShouldOrderBannersByPriority()
    {
        // Arrange
        var homePageBanners = new List<Banner>
        {
            CreateBannerWithType(1, BannerType.HomePage, 3),
            CreateBannerWithType(2, BannerType.HomePage, 1),
            CreateBannerWithType(3, BannerType.HomePage, 2)
        };

        _mockBannerRepository.Setup(r => r.FindAllAsync(
                It.Is<Expression<Func<Banner, bool>>>(expr => 
                    TestBannerTypeExpression(expr, BannerType.HomePage))))
            .ReturnsAsync(homePageBanners);

        _mockBannerRepository.Setup(r => r.FindAllAsync(
                It.Is<Expression<Func<Banner, bool>>>(expr => 
                    TestBannerTypeExpression(expr, BannerType.Featured))))
            .ReturnsAsync(new List<Banner>());

        _mockBannerRepository.Setup(r => r.FindAllAsync(
                It.Is<Expression<Func<Banner, bool>>>(expr => 
                    TestBannerTypeExpression(expr, BannerType.TopProducts))))
            .ReturnsAsync(new List<Banner>());

        // Act
        var result = await _sut.PrepareBannersForHomePage();

        // Assert
        result.HomePage.Should().BeInAscendingOrder(b => b.Priority);
        result.HomePage[0].Priority.Should().Be(1);
        result.HomePage[1].Priority.Should().Be(2);
        result.HomePage[2].Priority.Should().Be(3);
    }

    [Fact]
    public async Task PrepareBannersForHomePage_ShouldReturnEmptyLists_WhenNoBannersExist()
    {
        // Arrange
        _mockBannerRepository.Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<Banner, bool>>>()))
            .ReturnsAsync(new List<Banner>());

        // Act
        var result = await _sut.PrepareBannersForHomePage();

        // Assert
        result.HomePage.Should().BeEmpty();
        result.Featured.Should().BeEmpty();
        result.TopProducts.Should().BeEmpty();
    }

    [Fact]
    public async Task PrepareBannersForHomePage_ShouldCallRepositoryThreeTimes()
    {
        // Arrange
        _mockBannerRepository.Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<Banner, bool>>>()))
            .ReturnsAsync(new List<Banner>());

        // Act
        await _sut.PrepareBannersForHomePage();

        // Assert
        _mockBannerRepository.Verify(r => r.FindAllAsync(
            It.IsAny<Expression<Func<Banner, bool>>>()), Times.Exactly(3));
    }

    #endregion

    #region Helper Methods

    private static CreateBannerDto CreateValidCreateBannerDto()
    {
        return new CreateBannerDto
        {
            Title = "Test Banner",
            Subtitle = "Test Subtitle",
            Link = "/test",
            Priority = 1,
            BannerType = (int)BannerType.HomePage,
            Image = new byte[] { 1, 2, 3, 4, 5 },
            ImageUrl = "/banners",
            ImageType = ".jpg"
        };
    }

    private static UpdateBannerDto CreateValidUpdateBannerDto()
    {
        return new UpdateBannerDto
        {
            Id = 1,
            Title = "Updated Banner",
            Subtitle = "Updated Subtitle",
            Link = "/updated",
            Priority = 2,
            BannerType = (int)BannerType.Featured,
            Image = Array.Empty<byte>(),
            ImageUrl = "/banners",
            ImageType = ".jpg"
        };
    }

    private static Banner CreateBanner(int id, string title)
    {
        return new Banner
        {
            Id = id,
            Title = title,
            SubTitle = "Subtitle",
            ImageName = $"banner-{id}.jpg",
            Link = "/link",
            Priority = 1,
            BannerType = BannerType.HomePage
        };
    }

    private static Banner CreateBannerWithType(int id, BannerType bannerType, int priority)
    {
        return new Banner
        {
            Id = id,
            Title = $"Banner {id}",
            SubTitle = "Subtitle",
            ImageName = $"banner-{id}.jpg",
            Link = "/link",
            Priority = priority,
            BannerType = bannerType
        };
    }

    private static bool TestBannerTypeExpression(Expression<Func<Banner, bool>> expression, BannerType expectedType)
    {
        var testBanner = new Banner
        {
            Id = 1,
            Title = "Test",
            SubTitle = "Test",
            ImageName = "test.jpg",
            Link = "/test",
            Priority = 1,
            BannerType = expectedType
        };

        var compiledExpression = expression.Compile();
        return compiledExpression(testBanner);
    }

    #endregion
}
