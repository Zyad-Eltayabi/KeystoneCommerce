using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        private readonly DbSet<T> _entity;

        public GenericRepository(ApplicationDbContext applicationDbContext)
        {
            _context = applicationDbContext;
            _entity = _context.Set<T>();
        }


        public async Task AddAsync(T entity)
        {
            await _entity.AddAsync(entity);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _entity.AnyAsync(predicate);
        }

        public void Delete(T entity)
        {
            _entity.Remove(entity);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _entity.AsNoTracking().ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _entity.FindAsync(id);
        }

        public void Update(T entity)
        {
            _entity.Update(entity);
        }

        public async Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate)
        {
            var rowsAffected = await _entity.Where(predicate).ExecuteDeleteAsync();
            return rowsAffected > 0;
        }

        public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _entity.FirstOrDefaultAsync(predicate);
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate)
        {
            return await _entity.Where(predicate).ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _entity.CountAsync(predicate);
        }

        public async Task<int> CountAsync()
        {
            return await _entity.CountAsync();
        }
    }
}