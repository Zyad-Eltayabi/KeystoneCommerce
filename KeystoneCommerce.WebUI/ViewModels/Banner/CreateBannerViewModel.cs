using KeystoneCommerce.Domain.Enums;
using KeystoneCommerce.Shared.Constants;
using KeystoneCommerce.WebUI.Attributes;
using KeystoneCommerce.WebUI.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace KeystoneCommerce.WebUI.ViewModels.Banner;

public class CreateBannerViewModel : BaseBannerViewModel
{
    [Required(ErrorMessage = "Image is required")]
    [DataType(DataType.Upload)]
    [AllowedExtensions(FileExtensions.ImageExtensions, ErrorMessage = $"Please upload a valid image file ({FileExtensions.ImageExtensions})")]
    [MaxFileSize(FileSizes.MaxImageSizeInByte)]
    public IFormFile Image { get; set; } = null!;
}