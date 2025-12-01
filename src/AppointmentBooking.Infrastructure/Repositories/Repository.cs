using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AppointmentBooking.Core.Interfaces;
using AppointmentBooking.Infrastructure.Data;

namespace AppointmentBooking.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }
    
    /// <inheritdoc />
    /// <remarks>
    /// This method assumes the entity has an integer property named "Id".
    /// This is a common convention in EF Core. For entities with different
    /// primary key names or types, consider using a specialized repository.
    /// </remarks>
    public async Task<T?> GetByIdWithIncludesAsync(int id, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;
        
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        
        // Assumes the entity has an "Id" property - this is a common EF Core convention
        return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }
}
