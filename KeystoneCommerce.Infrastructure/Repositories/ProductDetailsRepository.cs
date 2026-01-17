using KeystoneCommerce.Application.DTOs.ProductDetails;
using KeystoneCommerce.Application.DTOs.Shop;
using KeystoneCommerce.Shared.Constants;

namespace KeystoneCommerce.Infrastructure.Repositories;

public class ProductDetailsRepository(ApplicationDbContext applicationDbContext)
    : GenericRepository<Product>(applicationDbContext), IProductDetailsRepository
{
    private readonly ApplicationDbContext _context = applicationDbContext;

    public async Task<List<ProductCardDto>?> GetNewArrivalsExcludingProduct(int productId)
    {
        var newArrivals = await _context.Products
            .Where(p => p.Id != productId)
            .Take(Products.CountOfNewArrivalsToShow)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductCardDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                ImageName = p.ImageName,
                Discount = p.Discount
            })
            .ToListAsync();
        return newArrivals;
    }

    public async Task<ProductDetailsDto?> GetProductDetailsByIdAsync(int productId)
    {
        var productDetails = await _context.Products
            .Where(p => p.Id == productId)
            .Select(p => new ProductDetailsDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                Discount = p.Discount,
                ImageName = p.ImageName,
                QTY = p.QTY,
                Tags = p.Tags,
                GalleryImageNames = p.Galleries
                    .Select(g => g.ImageName)
                    .ToList(),
                TotalReviews = p.Reviews.Count,
                NewArrivals = _context.Products
                    .Where(na => na.Id != productId)
                    .OrderByDescending(na => na.CreatedAt)
                    .Take(Products.CountOfNewArrivalsToShow)
                    .Select(na => new ProductCardDto
                    {
                        Id = na.Id,
                        Title = na.Title,
                        Price = na.Price,
                        ImageName = na.ImageName,
                        Discount = na.Discount
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
        return productDetails;
    }

}