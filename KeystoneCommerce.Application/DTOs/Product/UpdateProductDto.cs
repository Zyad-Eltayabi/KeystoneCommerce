using KeystoneCommerce.Application.DTOs.Common;

namespace KeystoneCommerce.Application.DTOs.Product
{
    public class UpdateProductDto : BaseProductDto
    {
        public int Id { get; set; }
        public ImageDto? MainImage { get; set; } = new();
        public List<ImageDto>? NewGalleries { get; set; } = new();
        public List<string>? DeletedImages { get; set; } = new();

        public bool HasNewGalleries => NewGalleries != null && NewGalleries.Count > 0;
        public bool HasDeletedImages => DeletedImages != null && DeletedImages.Count > 0;
    }
}
