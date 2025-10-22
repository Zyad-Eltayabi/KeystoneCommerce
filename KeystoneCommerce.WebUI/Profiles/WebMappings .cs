using AutoMapper;
using KeystoneCommerce.Application.DTOs;
using KeystoneCommerce.Application.DTOs.Banner;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Domain.Enums;
using KeystoneCommerce.WebUI.ViewModels.Banner;
using KeystoneCommerce.WebUI.ViewModels.Products;

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

            CreateMap<CreateProductViewModel, CreateProductDto>()
                .ForMember(e => e.MainImage, e => e.Ignore())
                .ForMember(e => e.Gallaries, e => e.Ignore())
                .ReverseMap();

            CreateMap<ProductDto, ProductViewModel>()
                .ReverseMap();

            CreateMap<ProductDto, EditProductViewModel>()
                .ForMember(p => p.ImageName, p => p.MapFrom(src => src.ImageName))
                .ForMember(p => p.GallaryImageNames, p => p.MapFrom(src => src.GalleryImageNames))
                .ForMember(p => p.MainImage, p => p.Ignore())
                .ForMember(p => p.Galleries, p => p.Ignore())
                .ForMember(p => p.HasNewMainImage, p => p.Ignore())
                .ForMember(p => p.HasNewGallaries, p => p.Ignore())
                .ForMember(p => p.DeletedImagesJson, p => p.Ignore());

            CreateMap<EditProductViewModel, UpdateProductDto>()
                .ForMember(e => e.MainImage, e => e.Ignore())
                .ForMember(e => e.NewGalleries, e => e.Ignore())
                .ForMember(e => e.HasNewGalleries, e => e.Ignore())
                .ForMember(e => e.HasDeletedImages, e => e.Ignore());
        }
    }
}