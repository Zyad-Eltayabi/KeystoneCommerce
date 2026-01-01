using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Infrastructure.Repositories;

public class ShopRepository(ApplicationDbContext applicationDbContext)
    : GenericRepository<Product>(applicationDbContext), IShopRepository
{
    private readonly ApplicationDbContext _context = applicationDbContext;

    public async Task<List<ProductCardDto>> GetAvailableProducts(PaginationParameters parameters)
    {
        var query = base.ConfigureQueryForPagination(parameters);

        parameters.TotalCount = await query.CountAsync();

        var products = await query
            .Where(p => p.QTY > 0)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
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
        return products;
    }
}