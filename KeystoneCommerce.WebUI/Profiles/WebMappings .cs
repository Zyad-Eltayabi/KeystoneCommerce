using AutoMapper;
using KeystoneCommerce.Application.DTOs;
using KeystoneCommerce.Application.DTOs.Account;
using KeystoneCommerce.Application.DTOs.Banner;
using KeystoneCommerce.Application.DTOs.Order;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.DTOs.ShippingDetails;
using KeystoneCommerce.Application.DTOs.ShippingMethod;
using KeystoneCommerce.Application.DTOs.Shop;
using KeystoneCommerce.Domain.Enums;
using KeystoneCommerce.WebUI.ViewModels.Account;
using KeystoneCommerce.WebUI.ViewModels.Banner;
using KeystoneCommerce.WebUI.ViewModels.Checkout;
using KeystoneCommerce.WebUI.ViewModels.Coupon;
using KeystoneCommerce.WebUI.ViewModels.OrderItem;
using KeystoneCommerce.WebUI.ViewModels.Orders;
using KeystoneCommerce.WebUI.ViewModels.Payment;
using KeystoneCommerce.WebUI.ViewModels.Products;
using KeystoneCommerce.WebUI.ViewModels.ShippingAddress;
using KeystoneCommerce.WebUI.ViewModels.ShippingMethod;
using KeystoneCommerce.WebUI.ViewModels.Shop;

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
                .ForMember(b => b.HasNewImage, b => b.Ignore())
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

            CreateMap<ProductCardDto, ProductCardViewModel>()
                .ReverseMap();

            CreateMap<RegisterViewModel, RegisterDto>()
                .ReverseMap();

            CreateMap<LoginViewModel, LoginDto>()
                .ReverseMap();

            CreateMap<ResetPasswordViewModel, ResetPasswordDto>()
                .ReverseMap();

            CreateMap<ShippingMethodDto, ViewModels.ShippingMethod.ShippingMethodViewModel>()
                .ReverseMap();

            CreateMap<ShippingDetailsViewModel, CreateShippingDetailsDto>()
                .ReverseMap();

            // Order Details Mappings
            CreateMap<OrderDetailsDto, OrderDetailsViewModel>()
                .ReverseMap();

            CreateMap<OrderItemDetailsDto, OrderItemViewModel>()
                .ReverseMap();

            CreateMap<ShippingAddressDetailsDto, ShippingAddressViewModel>()
                .ReverseMap();

            CreateMap<ShippingMethodDetailsDto, OrderShippingMethodViewModel>()
                .ReverseMap();

            CreateMap<PaymentDetailsDto, PaymentViewModel>()
                .ReverseMap();

            CreateMap<CouponDetailsDto, CouponViewModel>()
                .ReverseMap();

            CreateMap<UserBasicInfoDto, UserBasicViewModel>()
                .ReverseMap();

            // Order Analytics Mappings
            CreateMap<OrderAnalyticsDto, OrderAnalyticsViewModel>()
                .ReverseMap();

            CreateMap<OrderDashboardDto, OrderDashboardViewModel>()
                .ReverseMap();
        }
    }
}