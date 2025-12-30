using AutoMapper;
using KeystoneCommerce.Application.DTOs;
using KeystoneCommerce.Application.DTOs.Banner;
using KeystoneCommerce.Application.DTOs.Coupon;
using KeystoneCommerce.Application.DTOs.Order;
using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.DTOs.Review;
using KeystoneCommerce.Application.DTOs.ShippingDetails;
using KeystoneCommerce.Application.DTOs.ShippingMethod;
using KeystoneCommerce.Application.DTOs.Shop;
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

            CreateMap<Product, ProductDto>()
                .ForMember(e => e.GalleryImageNames, e => e.MapFrom(src => src.Galleries.Select(g => g.ImageName)));

            CreateMap<UpdateProductDto, Product>()
                .ForMember(e => e.CreatedAt, e => e.Ignore())
                .ForMember(e => e.UpdatedAt, e => e.Ignore())
                .ForMember(e => e.ImageName, e => e.Ignore())
                .ForMember(e => e.Galleries, e => e.Ignore())
                .ForMember(e => e.UpdatedAt, e => e.MapFrom(_ => DateTime.UtcNow));

            CreateMap<Review, ReviewDto>()
                .ReverseMap();

            CreateMap<Product, ProductCardDto>();

            CreateMap<ShippingMethod, ShippingMethodDto>()
                .ReverseMap();

            CreateMap<CreateShippingDetailsDto, ShippingAddress>();

            CreateMap<Coupon, CouponDto>();

            CreateMap<CreatePaymentDto, Payment>()
                .ForMember(e => e.Id, e => e.Ignore())
                .ForMember(e => e.CreatedAt, e => e.Ignore())
                .ForMember(e => e.UpdatedAt, e => e.Ignore())
                .ForMember(e => e.Order, e => e.Ignore());

            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.PaymentId, opt => opt.MapFrom(src => src.Payment != null ? src.Payment.Id : 0));
        }
    }
}
