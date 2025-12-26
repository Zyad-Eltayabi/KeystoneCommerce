using KeystoneCommerce.Application.DTOs.Coupon;
using System.Runtime.Serialization;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("CouponServiceTests")]
public class CouponServiceTest
{
    private readonly Mock<ICouponRepository> _mockCouponRepository;
    private readonly IMappingService _mappingService;
    private readonly Mock<ILogger<CouponService>> _mockLogger;
    private readonly CouponService _service;

    public CouponServiceTest()
    {
        _mockCouponRepository = new Mock<ICouponRepository>();
        _mappingService = new MappingService(MapperHelper.CreateMapper());
        _mockLogger = new Mock<ILogger<CouponService>>();

        _service = new CouponService(
            _mockCouponRepository.Object,
            _mappingService,
            _mockLogger.Object);
    }

    #region GetDiscountPercentageByCodeAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task GetDiscountPercentageByCodeAsync_ShouldReturnDiscountPercentage_WhenCouponExistsAndIsActive()
    {
        // Arrange
        string promoCode = "SUMMER2024";
        var coupon = CreateActiveCoupon(promoCode, 20);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetDiscountPercentageByCodeAsync(promoCode);

        // Assert
        result.Should().Be(20);
        _mockCouponRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()), Times.Once);
    }

    [Theory]
    [InlineData("WELCOME10", 10)]
    [InlineData("SALE50", 50)]
    [InlineData("DISCOUNT5", 5)]
    [InlineData("MEGASALE75", 75)]
    public async Task GetDiscountPercentageByCodeAsync_ShouldReturnCorrectPercentage_ForDifferentCoupons(
        string promoCode, int discount)
    {
        // Arrange
        var coupon = CreateActiveCoupon(promoCode, discount);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetDiscountPercentageByCodeAsync(promoCode);

        // Assert
        result.Should().Be(discount);
    }

    [Fact]
    public async Task GetDiscountPercentageByCodeAsync_ShouldHandleDefaultParameter_Successfully()
    {
        // Arrange
        var coupon = CreateActiveCoupon("0", 15);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetDiscountPercentageByCodeAsync(); // Using default parameter

        // Assert
        result.Should().Be(15);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(99)]
    [InlineData(100)]
    public async Task GetDiscountPercentageByCodeAsync_ShouldHandleDifferentDiscountValues_Successfully(int discount)
    {
        // Arrange
        string promoCode = "TEST";
        var coupon = CreateActiveCoupon(promoCode, discount);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetDiscountPercentageByCodeAsync(promoCode);

        // Assert
        result.Should().Be(discount);
    }

    #endregion

    #region Validation and Edge Cases

    [Fact]
    public async Task GetDiscountPercentageByCodeAsync_ShouldReturnZero_WhenCouponNotFound()
    {
        // Arrange
        string promoCode = "NONEXISTENT";

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync((Coupon?)null);

        // Act
        var result = await _service.GetDiscountPercentageByCodeAsync(promoCode);

        // Assert
        result.Should().Be(0);
        _mockCouponRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetDiscountPercentageByCodeAsync_ShouldReturnZero_WhenCouponIsInactive()
    {
        // Arrange
        string promoCode = "EXPIRED";
        var coupon = CreateInactiveCoupon(promoCode, 30);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetDiscountPercentageByCodeAsync(promoCode);

        // Assert
        result.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("VALID123")]
    public async Task GetDiscountPercentageByCodeAsync_ShouldHandleVariousCodes_Gracefully(string promoCode)
    {
        // Arrange
        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync((Coupon?)null);

        // Act
        var result = await _service.GetDiscountPercentageByCodeAsync(promoCode);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetDiscountPercentageByCodeAsync_ShouldHandleSpecialCharacters_InPromoCode()
    {
        // Arrange
        string promoCode = "PROMO-2024!";
        var coupon = CreateActiveCoupon(promoCode, 15);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetDiscountPercentageByCodeAsync(promoCode);

        // Assert
        result.Should().Be(15);
    }

    [Fact]
    public async Task GetDiscountPercentageByCodeAsync_ShouldHandleVeryLongPromoCode_Successfully()
    {
        // Arrange
        string promoCode = new string('A', 100); // Max length based on configuration
        var coupon = CreateActiveCoupon(promoCode, 20);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetDiscountPercentageByCodeAsync(promoCode);

        // Assert
        result.Should().Be(20);
    }

    #endregion

    #endregion

    #region GetCouponByName Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task GetCouponByName_ShouldReturnSuccess_WhenCouponExistsAndIsActive()
    {
        // Arrange
        string couponName = "SUMMER2024";
        var coupon = CreateActiveCoupon(couponName, 20);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetCouponByName(couponName);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Code.Should().Be(couponName);
        result.Data.DiscountPercentage.Should().Be(20);
        result.Errors.Should().BeEmpty();

        _mockCouponRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()), Times.Once);
    }

    [Theory]
    [InlineData("WELCOME10")]
    [InlineData("SALE50")]
    [InlineData("FLASH75")]
    public async Task GetCouponByName_ShouldReturnCorrectCoupon_ForDifferentNames(string couponName)
    {
        // Arrange
        var coupon = CreateActiveCoupon(couponName, 25);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetCouponByName(couponName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Code.Should().Be(couponName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task GetCouponByName_ShouldHandleDifferentDiscountPercentages_Successfully(int discount)
    {
        // Arrange
        string couponName = "TEST";
        var coupon = CreateActiveCoupon(couponName, discount);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetCouponByName(couponName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.DiscountPercentage.Should().Be(discount);
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task GetCouponByName_ShouldReturnFailure_WhenCouponNameIsNull()
    {
        // Arrange
        string? couponName = null;

        // Act
        var result = await _service.GetCouponByName(couponName!);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Coupon code cannot be empty.");
        result.Data.Should().BeNull();

        _mockCouponRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GetCouponByName_ShouldReturnFailure_WhenCouponNameIsEmpty()
    {
        // Arrange
        string couponName = "";

        // Act
        var result = await _service.GetCouponByName(couponName);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Coupon code cannot be empty.");
        result.Data.Should().BeNull();

        _mockCouponRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(TestData.WhiteSpaceTestData), MemberType = typeof(TestData))]
    public async Task GetCouponByName_ShouldReturnFailure_WhenCouponNameIsWhitespace(string couponName)
    {
        // Arrange & Act
        var result = await _service.GetCouponByName(couponName);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Coupon code cannot be empty.");
        result.Data.Should().BeNull();

        _mockCouponRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GetCouponByName_ShouldReturnFailure_WhenCouponNotFound()
    {
        // Arrange
        string couponName = "NONEXISTENT";

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync((Coupon?)null);

        // Act
        var result = await _service.GetCouponByName(couponName);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Coupon not found.");
        result.Data.Should().BeNull();

        _mockCouponRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetCouponByName_ShouldReturnFailure_WhenCouponIsInactive()
    {
        // Arrange
        string couponName = "EXPIRED";
        var coupon = CreateInactiveCoupon(couponName, 30);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetCouponByName(couponName);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("This coupon has expired.");
        result.Data.Should().BeNull();

        _mockCouponRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetCouponByName_ShouldHandleSpecialCharacters_InCouponCode()
    {
        // Arrange
        string couponName = "PROMO-2024!@#";
        var coupon = CreateActiveCoupon(couponName, 15);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetCouponByName(couponName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Code.Should().Be(couponName);
    }

    [Fact]
    public async Task GetCouponByName_ShouldHandleMaxLengthCouponCode_Successfully()
    {
        // Arrange
        string couponName = new string('A', 100); // Max length from configuration
        var coupon = CreateActiveCoupon(couponName, 30);

        _mockCouponRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Coupon, bool>>>()))
            .ReturnsAsync(coupon);

        // Act
        var result = await _service.GetCouponByName(couponName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Code.Should().HaveLength(100);
    }

    #endregion

    #endregion

    #region Helper Methods

    private static Coupon CreateActiveCoupon(string code, int discountPercentage)
    {
        return new Coupon
        {
            Id = 1,
            Code = code,
            DiscountPercentage = discountPercentage,
            StartAt = DateTime.UtcNow.AddDays(-1),
            EndAt = DateTime.UtcNow.AddDays(30), // Active for 30 more days
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
    }

    private static Coupon CreateInactiveCoupon(string code, int discountPercentage)
    {
        return new Coupon
        {
            Id = 1,
            Code = code,
            DiscountPercentage = discountPercentage,
            StartAt = DateTime.UtcNow.AddDays(-30),
            EndAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            CreatedAt = DateTime.UtcNow.AddDays(-31)
        };
    }

    #endregion
}
