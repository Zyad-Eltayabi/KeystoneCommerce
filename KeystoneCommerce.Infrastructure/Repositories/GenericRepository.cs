using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        protected readonly DbSet<T> Entity;

        public GenericRepository(ApplicationDbContext applicationDbContext)
        {
            _context = applicationDbContext;
            Entity = _context.Set<T>();
        }


        public async Task AddAsync(T entity)
        {
            await Entity.AddAsync(entity);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await Entity.AnyAsync(predicate);
        }

        public void Delete(T entity)
        {
            Entity.Remove(entity);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await Entity.AsNoTracking().ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await Entity.FindAsync(id);
        }

        public void Update(T entity)
        {
            Entity.Update(entity);
        }

        public async Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate)
        {
            var rowsAffected = await Entity.Where(predicate).ExecuteDeleteAsync();
            return rowsAffected > 0;
        }

        public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await Entity.FirstOrDefaultAsync(predicate);
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate)
        {
            return await Entity.Where(predicate).ToListAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
           return await _context.SaveChangesAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await Entity.CountAsync(predicate);
        }

        public async Task<int> CountAsync()
        {
            return await Entity.CountAsync();
        }
    }
}