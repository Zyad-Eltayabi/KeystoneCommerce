using AutoMapper;
using KeystoneCommerce.Application.DTOs;
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
        }
    }
}
