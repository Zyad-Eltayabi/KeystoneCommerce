using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Shared.Constants;
using System.ComponentModel;
using System.Linq.Dynamic.Core;
using System.Reflection;

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

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> expression, bool tracking = false)
        {
            var query = Entity.AsQueryable();
            if (!tracking)
                query = query.AsNoTracking();
            return await query.Where(expression).ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await Entity.FindAsync(id);
        }
        public async Task<T?> GetByIdAsync(Expression<Func<T, bool>> expression, List<Expression<Func<T, object>>> includes)
        {
            IQueryable<T> query = _context.Set<T>();
            if (includes is not null && includes.Any())
                includes.ForEach(include => query = query.Include(include));
            return await query.FirstOrDefaultAsync(expression);
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

        public async Task<List<T>> GetPagedAsync(PaginationParameters parameters)
        {
            var query = ConfigureQueryForPagination(parameters);
            parameters.TotalCount = await query.CountAsync();
            return await query.AsNoTracking()
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();
        }

        protected IQueryable<T> ConfigureQueryForPagination(PaginationParameters parameters)
        {
            var query = Entity.AsQueryable();

            if (!string.IsNullOrEmpty(parameters.SortBy))
            {
                var property = typeof(T).GetProperty(parameters.SortBy,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property == null)
                    parameters.SortBy = "Id";

                query = parameters.SortOrder?.ToLower() == Sorting.Descending.ToLower()
                    ? query.OrderByDescending(e => EF.Property<object>(e, parameters.SortBy))
                    : query.OrderBy(e => EF.Property<object>(e, parameters.SortBy));
            }

            if (!string.IsNullOrEmpty(parameters.SearchBy) &&
                !string.IsNullOrEmpty(parameters.SearchValue))
            {
                var property = typeof(T).GetProperty(parameters.SearchBy,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property is not null)
                {
                    Type? propertyType = property.PropertyType;
                    string expression;

                    if (propertyType == typeof(string))
                        expression = $"{parameters.SearchBy}.Contains(@0)";
                    else
                        expression = $"{parameters.SearchBy} == @0";

                    if (TryConvertUsingTypeConverter(parameters.SearchValue, propertyType, out var convertedValue))
                    {
                        var lambda = DynamicExpressionParser.ParseLambda<T, bool>(
                            new ParsingConfig(), true, expression, convertedValue);
                        query = query.Where(lambda);
                    }
                }
            }

            return query;
        }

        private bool TryConvertUsingTypeConverter(string? input, Type targetType, out object? convertedValue)
        {
            convertedValue = null;

            if (string.IsNullOrWhiteSpace(input))
                return (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null) ? true : false;

            var converter = TypeDescriptor.GetConverter(targetType);

            if (!converter.CanConvertFrom(typeof(string)))
                return false;

            try
            {
                object value = converter.ConvertFromInvariantString(input)!;

                if (!targetType.IsAssignableFrom(value.GetType()))
                    return false;

                convertedValue = value;
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}