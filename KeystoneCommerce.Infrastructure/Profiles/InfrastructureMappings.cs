using AutoMapper;
using KeystoneCommerce.Application.DTOs;
using KeystoneCommerce.Application.DTOs.Banner;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Domain.Enums;
namespace KeystoneCommerce.Infrastructure.Profiles
{
    public class InfrastructureMappings : Profile
    {
        public InfrastructureMappings()
        {
            CreateMap<CreateBannerDto, Banner>()
                .ForMember(e => e.BannerType, e => e.MapFrom(src => (BannerType)src.BannerType))
                .ReverseMap();
            
            CreateMap<UpdateBannerDto, Banner>()
                .ForMember(e => e.BannerType, e => e.MapFrom(src => (BannerType)src.BannerType))
                .ReverseMap();

            CreateMap<Banner, BannerDto>()
                .ForMember(e => e.BannerType, e => e.MapFrom(src => Enum.GetName(typeof(BannerType), src.BannerType)));

            CreateMap<CreateProductDto, Product>()
                .ForMember(e => e.UpdatedAt, e => e.Ignore())
                .ForMember(e => e.ImageName, e => e.Ignore())
                .ForMember(e => e.Galleries, e => e.Ignore());
                

        }
    }
}
