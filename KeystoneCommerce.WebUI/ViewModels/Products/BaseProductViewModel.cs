using System.ComponentModel.DataAnnotations;

namespace KeystoneCommerce.WebUI.ViewModels.Products
{
    public class BaseProductViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Summary is required")]
        [MaxLength(500, ErrorMessage = "Summary cannot exceed 500 characters")]
        public string Summary { get; set; } = null!;

        [Required(ErrorMessage = "Description is required")]
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount must be greater than or equal to 0")]
        public decimal? Discount { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 1")]
        public int QTY { get; set; }

        [MaxLength(1000, ErrorMessage = "Tags cannot exceed 1000 characters")]
        public string? Tags { get; set; }
    }
}
