using AutoMapper;
using KeystoneCommerce.Application.DTOs;
using KeystoneCommerce.Application.DTOs.Banner;
using KeystoneCommerce.Domain.Enums;
using KeystoneCommerce.WebUI.ViewModels.Banner;

namespace KeystoneCommerce.WebUI.Profiles
{
    public class WebMappings : Profile
    {
        public WebMappings()
        {
            CreateMap<CreateBannerViewModel, CreateBannerDto>()
                .ForMember(e => e.Image, e => e.Ignore())
                .ForMember(e => e.ImageUrl, e => e.Ignore())
                .ForMember(e => e.ImageType, e => e.Ignore())
                .ReverseMap();
            
            CreateMap<UpdateBannerViewModel, UpdateBannerDto>()
                .ForMember(e => e.Image, e => e.Ignore())
                .ForMember(e => e.ImageUrl, e => e.Ignore())
                .ForMember(e => e.ImageType, e => e.Ignore())
                .ReverseMap();

            CreateMap<BannerDto, BannerViewModel>()
                .ReverseMap();

            CreateMap<BannerDto, UpdateBannerViewModel>()
                .ForMember(b => b.Image, b => b.Ignore())
                .ForMember(b=>b.HasNewImage, b=>b.Ignore())
                .ForMember(b => b.BannerType, e => e.MapFrom(src => 
                    (int)Enum.Parse<BannerType>(src.BannerType)))
                .ReverseMap();
        }
    }
}