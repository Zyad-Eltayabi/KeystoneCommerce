namespace KeystoneCommerce.WebUI.ViewModels.Cart
{
    public class ProductCartViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ImageName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Price { get; set; }
        public decimal? RowSumPrice { get; set; }
    }
}
