using Microsoft.EntityFrameworkCore;

namespace KeystoneCommerce.Infrastructure.Persistence.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
};