using KeystoneCommerce.Application.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace KeystoneCommerce.Application.DTOs.Product
{
    public class CreateProductDto : BaseProductDto
    {
        public ImageDto MainImage { get; set; } = null!;
        public List<ImageDto> Gallaries { get; set; } = new();
    }
}
