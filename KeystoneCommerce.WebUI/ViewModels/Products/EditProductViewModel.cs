using KeystoneCommerce.Shared.Constants;
using KeystoneCommerce.WebUI.Attributes;
using KeystoneCommerce.WebUI.Constants;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace KeystoneCommerce.WebUI.ViewModels.Products
{
    public class EditProductViewModel : BaseProductViewModel
    {
        public int Id { get; set; }
        public string? ImageName { get; set; }
        public List<string>? GallaryImageNames { get; set; } = new List<string>();

        [DataType(DataType.Upload)]
        [AllowedExtensions(FileExtensions.ImageExtensions, ErrorMessage = $"Please upload a valid image file ({FileExtensions.ImageExtensions})")]
        [MaxFileSize(FileSizes.MaxImageSizeInByte)]
        public IFormFile? MainImage { get; set; }

        [DataType(DataType.Upload)]
        [AllowedExtensions(FileExtensions.ImageExtensions, ErrorMessage = $"Please upload valid image files ({FileExtensions.ImageExtensions})")]
        [MaxFileSize(FileSizes.MaxImageSizeInByte)]
        [MaxUploadImagesCount(FileSizes.MaxNumberOfGalleryImages, ErrorMessage = "The number of images exceed the limit")]
        public IFormFile[]? Galleries { get; set; }
        public string? DeletedImagesJson { get; set; }

        [NotMapped]
        public List<string>? DeletedImages
            => string.IsNullOrEmpty(DeletedImagesJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(DeletedImagesJson);

        public bool HasNewMainImage => MainImage != null && MainImage.Length > 0;
        public bool HasNewGallaries => Galleries != null && Galleries.Length > 0;
    }
}
