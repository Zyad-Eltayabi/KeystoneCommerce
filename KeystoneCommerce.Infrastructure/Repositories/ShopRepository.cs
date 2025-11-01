using KeystoneCommerce.Application.DTOs.Shop;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace KeystoneCommerce.Infrastructure.Repositories;

public class ShopRepository(ApplicationDbContext applicationDbContext)
    : GenericRepository<Product>(applicationDbContext), IShopRepository
{
    private readonly ApplicationDbContext _context = applicationDbContext;

    public async Task<List<ProductCardDto>> GetAvailableProducts()
    {
        var products = await _context.Products
            .Where(p => p.QTY > 0)
            .Select(p => new ProductCardDto
            {
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                ImageName = p.ImageName,
                Discount = p.Discount
            })
            .ToListAsync();
        return products;
    }
}