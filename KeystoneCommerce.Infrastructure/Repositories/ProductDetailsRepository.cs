using KeystoneCommerce.Application.DTOs.Shop;
using KeystoneCommerce.Shared.Constants;

namespace KeystoneCommerce.Infrastructure.Repositories;

public class ProductDetailsRepository(ApplicationDbContext applicationDbContext)
    : GenericRepository<Product>(applicationDbContext), IProductDetailsRepository
{
    private readonly ApplicationDbContext _context = applicationDbContext;

    public async Task<List<ProductCardDto>?> GetNewArrivalsExcludingProduct(int productId)
    {
        var newArrivals =await _context.Products
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
}