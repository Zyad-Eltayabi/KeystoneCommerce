using KeystoneCommerce.Domain.Enums;
using KeystoneCommerce.WebUI.Attributes;
using KeystoneCommerce.WebUI.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace KeystoneCommerce.WebUI.ViewModels.Banner;

public class CreateBannerViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sub Title is required")]
    [MaxLength(500, ErrorMessage = "Sub title cannot exceed 500 characters")]
    public string SubTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Link is required")]
    [MaxLength(100, ErrorMessage = "Link cannot exceed 100 characters")]
    public string Link { get; set; } = string.Empty;

    [Required(ErrorMessage = "Priority is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Priority must be a positive integer")]
    public int Priority { get; set; }

    [Required(ErrorMessage = "Banner Type is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid banner type.")]
    public int BannerType { get; set; }


    public List<SelectListItem> BannerTypeNames { get; set; } = new List<SelectListItem>();

    [Required(ErrorMessage = "Image is required")]
    [DataType(DataType.Upload)]
    [AllowedExtensions(FileExtensions.ImageExtensions, ErrorMessage = $"Please upload a valid image file ({FileExtensions.ImageExtensions})")]
    [MaxFileSize(FileSizes.MaxImageSizeInByte)]
    public IFormFile Image { get; set; } = null!;
}