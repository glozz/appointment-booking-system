using System.Linq.Expressions;

namespace AppointmentBooking.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    
    /// <summary>
    /// Returns a queryable for LINQ operations including Include() for eager loading.
    /// </summary>
    IQueryable<T> Query();
    
    /// <summary>
    /// Gets an entity by ID with specified navigation properties eagerly loaded.
    /// </summary>
    Task<T?> GetByIdWithIncludesAsync(int id, params Expression<Func<T, object>>[] includes);
}
