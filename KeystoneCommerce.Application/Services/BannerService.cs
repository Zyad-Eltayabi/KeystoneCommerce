using KeystoneCommerce.Application.DTOs.Banner;

namespace KeystoneCommerce.Application.Services;

public class BannerService : IBannerService
{
    private readonly IImageService _imageService;
    private readonly IBannerRepository _bannerRepository;
    private readonly IMappingService _mappingService;
    private readonly IApplicationValidator<CreateBannerDto> _createValidator;
    private readonly IApplicationValidator<UpdateBannerDto> _updateValidator;
    private readonly ILogger<BannerService> _logger;


    public BannerService(IImageService imageService, IBannerRepository bannerRepository,
        IMappingService mappingService,
        IApplicationValidator<CreateBannerDto> createValidator,
        IApplicationValidator<UpdateBannerDto> updateValidator,
        ILogger<BannerService> logger)
    {
        _imageService = imageService;
        _bannerRepository = bannerRepository;
        _mappingService = mappingService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<Result<bool>> Create(CreateBannerDto createBannerDto)
    {
        _logger.LogInformation(
            "Creating banner. BannerType: {BannerType}, Priority: {Priority}, ImageType: {ImageType}",
            createBannerDto.BannerType,
            createBannerDto.Priority,
            createBannerDto.ImageType);

        var validation = _createValidator.Validate(createBannerDto);
        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "Banner creation validation failed. BannerType: {BannerType}, ValidationErrors: {@ValidationErrors}",
                createBannerDto.BannerType,
                validation.Errors);
            return Result<bool>.Failure(validation.Errors);
        }

        var banner = await PrepareBannerWithImage(createBannerDto);
        await _bannerRepository.AddAsync(banner);
        await _bannerRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Banner created successfully. BannerId: {BannerId}, BannerType: {BannerType}, Priority: {Priority}, ImageName: {ImageName}",
            banner.Id,
            banner.BannerType,
            banner.Priority,
            banner.ImageName);

        return Result<bool>.Success();
    }

    public Dictionary<int, string> GetBannerTypes()
    {
        _logger.LogInformation("Retrieving banner types enumeration");

        Dictionary<int, string> result = [];
        var bannerEnumValues = Enum.GetValues(typeof(BannerType));
        foreach (BannerType bannerEnumValue in bannerEnumValues)
            result.Add((int)bannerEnumValue, bannerEnumValue.ToString());

        _logger.LogInformation(
            "Banner types retrieved successfully. TotalTypes: {TotalTypes}",
            result.Count);

        return result;
    }

    public async Task<List<BannerDto>> GetBanners()
    {
        _logger.LogInformation("Retrieving all banners");

        var banners = await _bannerRepository.GetAllAsync();
        var result = _mappingService.Map<List<BannerDto>>(banners);

        _logger.LogInformation(
            "All banners retrieved successfully. TotalBanners: {TotalBanners}",
            result.Count);

        return result;
    }

    public async Task<BannerDto?> GetById(int id)
    {
        _logger.LogInformation(
            "Retrieving banner by ID. BannerId: {BannerId}",
            id);

        var banner = await _bannerRepository.GetByIdAsync(id);

        if (banner is null)
        {
            _logger.LogWarning(
                "Banner not found. BannerId: {BannerId}",
                id);
            return null;
        }

        var result = _mappingService.Map<BannerDto>(banner);

        _logger.LogInformation(
            "Banner retrieved successfully. BannerId: {BannerId}, BannerType: {BannerType}, Priority: {Priority}",
            id,
            result.BannerType,
            result.Priority);

        return result;
    }

    public async Task<Result<bool>> UpdateBannerAsync(UpdateBannerDto updateBannerDto)
    {
        _logger.LogInformation(
            "Updating banner. BannerId: {BannerId}, BannerType: {BannerType}, Priority: {Priority}, HasNewImage: {HasNewImage}",
            updateBannerDto.Id,
            updateBannerDto.BannerType,
            updateBannerDto.Priority,
            updateBannerDto.Image?.Length > 0);

        var validation = _updateValidator.Validate(updateBannerDto);
        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "Banner update validation failed. BannerId: {BannerId}, ValidationErrors: {@ValidationErrors}",
                updateBannerDto.Id,
                validation.Errors);
            return Result<bool>.Failure(validation.Errors);
        }

        var banner = await _bannerRepository.GetByIdAsync(updateBannerDto.Id);
        if (banner is null)
        {
            _logger.LogWarning(
                "Update failed. Banner not found. BannerId: {BannerId}",
                updateBannerDto.Id);
            return Result<bool>.Failure("Banner not found!");
        }

        var oldImageName = banner.ImageName;
        if (updateBannerDto.Image.Length > 0)
        {
            _logger.LogInformation(
                "Updating banner image. BannerId: {BannerId}, OldImageName: {OldImageName}",
                updateBannerDto.Id,
                oldImageName);

            var newImageName = await _imageService.SaveImageAsync(updateBannerDto.Image,
                updateBannerDto.ImageType, updateBannerDto.ImageUrl);
            banner.ImageName = newImageName;

            _logger.LogInformation(
                "Banner image updated. BannerId: {BannerId}, NewImageName: {NewImageName}",
                updateBannerDto.Id,
                newImageName);
        }

        await MapAndUpdateBanner(updateBannerDto, banner);

        if (oldImageName != banner.ImageName)
        {
            _logger.LogInformation(
                "Deleting old banner image. BannerId: {BannerId}, OldImageName: {OldImageName}",
                updateBannerDto.Id,
                oldImageName);

            await _imageService.DeleteImageAsync(updateBannerDto.ImageUrl, oldImageName);

            _logger.LogInformation(
                "Old banner image deleted successfully. BannerId: {BannerId}, OldImageName: {OldImageName}",
                updateBannerDto.Id,
                oldImageName);
        }

        _logger.LogInformation(
            "Banner updated successfully. BannerId: {BannerId}, BannerType: {BannerType}, Priority: {Priority}, ImageChanged: {ImageChanged}",
            updateBannerDto.Id,
            updateBannerDto.BannerType,
            updateBannerDto.Priority,
            oldImageName != banner.ImageName);

        return Result<bool>.Success();
    }

    public async Task<Result<bool>> DeleteBannerAsync(int id, string imageUrl)
    {
        _logger.LogInformation(
            "Deleting banner. BannerId: {BannerId}",
            id);

        var banner = await _bannerRepository.GetByIdAsync(id);
        if (banner is null)
        {
            _logger.LogWarning(
                "Delete failed. Banner not found. BannerId: {BannerId}",
                id);
            return Result<bool>.Failure("Banner not found!");
        }

        var bannerImageName = banner.ImageName;
        var bannerType = banner.BannerType;
        var bannerPriority = banner.Priority;

        _bannerRepository.Delete(banner);
        int rowsAffected = await _bannerRepository.SaveChangesAsync();

        if (rowsAffected == 0)
        {
            _logger.LogError(
                "Failed to delete banner from database. BannerId: {BannerId}, RowsAffected: {RowsAffected}",
                id,
                rowsAffected);
            return Result<bool>.Failure("failed to delete banner");
        }

        _logger.LogInformation(
            "Banner deleted from database successfully. BannerId: {BannerId}, BannerType: {BannerType}, Priority: {Priority}, ImageName: {ImageName}",
            id,
            bannerType,
            bannerPriority,
            bannerImageName);

        await _imageService.DeleteImageAsync(imageUrl, bannerImageName);

        _logger.LogInformation(
            "Banner and associated image deleted successfully. BannerId: {BannerId}, ImageName: {ImageName}",
            id,
            bannerImageName);

        return Result<bool>.Success();
    }

    public async Task<HomeBannersDto> PrepareBannersForHomePage()
    {
        _logger.LogInformation("Preparing banners for home page");

        var homeBanners = new HomeBannersDto
        {
            HomePage = _mappingService.Map<List<BannerDto>>(
                await _bannerRepository.FindAllAsync(b => b.BannerType == BannerType.HomePage)
                ).OrderBy(b => b.Priority).ToList(),
            Featured = _mappingService.Map<List<BannerDto>>(
                await _bannerRepository.FindAllAsync(b => b.BannerType == BannerType.Featured)).OrderBy(b => b.Priority).ToList(),
            TopProducts = _mappingService.Map<List<BannerDto>>(
                await _bannerRepository.FindAllAsync(b =>
                    b.BannerType == BannerType.TopProducts)).OrderBy(b => b.Priority).ToList()
        };

        _logger.LogInformation(
            "Home page banners prepared successfully. HomePageBanners: {HomePageBannersCount}, FeaturedBanners: {FeaturedBannersCount}, TopProductsBanners: {TopProductsBannersCount}",
            homeBanners.HomePage.Count,
            homeBanners.Featured.Count,
            homeBanners.TopProducts.Count);

        return homeBanners;
    }

    private async Task MapAndUpdateBanner(UpdateBannerDto updateBannerDto, Banner banner)
    {
        _mappingService.Map(updateBannerDto, banner);
        _bannerRepository.Update(banner);
        await _bannerRepository.SaveChangesAsync();
    }

    private async Task<Banner> PrepareBannerWithImage(CreateBannerDto createBannerDto)
    {
        _logger.LogInformation(
            "Preparing banner with image. ImageType: {ImageType}, HasImageData: {HasImageData}",
            createBannerDto.ImageType,
            createBannerDto.Image?.Length > 0);

        var imageName = await _imageService.SaveImageAsync(createBannerDto.Image,
            createBannerDto.ImageType, createBannerDto.ImageUrl);
        var banner = _mappingService.Map<Banner>(createBannerDto);
        banner.ImageName = imageName;

        _logger.LogInformation(
            "Banner prepared with image successfully. ImageName: {ImageName}, BannerType: {BannerType}",
            imageName,
            createBannerDto.BannerType);

        return banner;
    }
}
