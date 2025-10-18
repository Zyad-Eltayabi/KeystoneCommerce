using KeystoneCommerce.Shared.Constants;
using KeystoneCommerce.WebUI.Attributes;
using KeystoneCommerce.WebUI.Constants;
using System.ComponentModel.DataAnnotations;

namespace KeystoneCommerce.WebUI.ViewModels.Products
{
    public class CreateProductViewModel : BaseProductViewModel
    {
        [Required(ErrorMessage = "Main image is required")]
        [DataType(DataType.Upload)]
        [AllowedExtensions(FileExtensions.ImageExtensions, ErrorMessage = $"Please upload a valid image file ({FileExtensions.ImageExtensions})")]
        [MaxFileSize(FileSizes.MaxImageSizeInByte)]
        public IFormFile MainImage { get; set; } = null!;

        [DataType(DataType.Upload)]
        [AllowedExtensions(FileExtensions.ImageExtensions, ErrorMessage = $"Please upload valid image files ({FileExtensions.ImageExtensions})")]
        [MaxFileSize(FileSizes.MaxImageSizeInByte)]
        [MaxUploadImagesCount(FileSizes.MaxNumberOfGalleryImages, ErrorMessage = "The number of images exceed the limit")]
        public IFormFile[] Gallaries { get; set; } = null!;
    }
}
