using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace KeystoneCommerce.Infrastructure.Repositories;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private IDbContextTransaction _transaction = null!;

    public async Task BeginTransactionAsync()
    {
        _transaction = await context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        await _transaction.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        await _transaction.RollbackAsync();
    }
}
