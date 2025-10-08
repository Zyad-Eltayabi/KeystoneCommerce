using System.ComponentModel.DataAnnotations;
using KeystoneCommerce.WebUI.Attributes;
using KeystoneCommerce.WebUI.Constants;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.ViewModels.Banner;

public class UpdateBannerViewModel : BaseBannerViewModel
{
    [HiddenInput]
    [Required(ErrorMessage = "ID is required")]
    public int Id { get; set; }
    public bool HasNewImage => Image != null && Image.Length > 0;
    
    [DataType(DataType.Upload)]
    [AllowedExtensions(FileExtensions.ImageExtensions, ErrorMessage = $"Please upload a valid image file ({FileExtensions.ImageExtensions})")]
    [MaxFileSize(FileSizes.MaxImageSizeInByte)]
    public new IFormFile? Image { get; set; }
}