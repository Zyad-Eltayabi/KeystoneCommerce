using System.Linq.Expressions;
using KeystoneCommerce.Application.Common.Pagination;

namespace KeystoneCommerce.Application.Interfaces.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<IEnumerable<T>> GetAllAsync();
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        public Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync();
        Task<int> SaveChangesAsync();
        Task<List<T>> GetPagedAsync(PaginationParameters parameters);

    }
}