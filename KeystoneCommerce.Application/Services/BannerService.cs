using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Application.Services
{
    public class BannerService : IBannerService
    {
        private readonly IImageService _imageService;
        private readonly IBannerRepository _bannerRepository;
        private readonly IMappingService _mappingService;
        private readonly IApplicationValidator<CreateBannerDto> _validationService;
        public BannerService(IImageService imageService, IBannerRepository bannerRepository, IMappingService mappingService, IApplicationValidator<CreateBannerDto> validationService)
        {
            _imageService = imageService;
            _bannerRepository = bannerRepository;
            _mappingService = mappingService;
            _validationService = validationService;
        }

        public async Task<Result<bool>> Create(CreateBannerDto createBannerDto)
        {
            var validation = _validationService.Validate(createBannerDto);
            if (!validation.IsValid)
               return Result<bool>.Failure(validation.Errors);
            var banner = await PrepareBannerWithImage(createBannerDto);
            await _bannerRepository.AddAsync(banner);
            await _bannerRepository.SaveChangesAsync();
            return Result<bool>.Success();
        }

        private async Task<Banner> PrepareBannerWithImage(CreateBannerDto createBannerDto)
        {
            var imageName = await _imageService.SaveImageAsync(createBannerDto.Image, createBannerDto.ImageType, createBannerDto.ImageUrl);
            var banner = _mappingService.Map<Banner>(createBannerDto);
            banner.ImageName = imageName;
            return banner;
        }

        public Dictionary<int, string> GetBannerTypes()
        {
            Dictionary<int, string> result = new();
            var bannerEnumValues = Enum.GetValues(typeof(BannerType));
            foreach (BannerType bannerEnumValue in bannerEnumValues)
                result.Add((int)bannerEnumValue, bannerEnumValue.ToString());
            return result;
        }
    }
}
