using Microsoft.EntityFrameworkCore;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;
using AppointmentBooking.Infrastructure.Data;

namespace AppointmentBooking.Infrastructure.Repositories;

/// <summary>
/// Specialized repository for Appointment entities with optimized queries including eager loading.
/// </summary>
public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(AppDbContext context) : base(context)
    {
    }
    
    /// <inheritdoc />
    public async Task<Appointment?> GetByIdWithIncludesAsync(int id)
    {
        return await _dbSet
            .Include(a => a.Branch)
            .Include(a => a.Service)
            .Include(a => a.Customer)
            .Include(a => a.Consultant)
            .FirstOrDefaultAsync(a => a.Id == id);
    }
    
    /// <inheritdoc />
    public async Task<Appointment?> GetByConfirmationCodeWithIncludesAsync(string confirmationCode)
    {
        return await _dbSet
            .Include(a => a.Branch)
            .Include(a => a.Service)
            .Include(a => a.Customer)
            .Include(a => a.Consultant)
            .FirstOrDefaultAsync(a => a.ConfirmationCode == confirmationCode);
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<Appointment>> GetByCustomerIdWithIncludesAsync(int customerId)
    {
        return await _dbSet
            .Include(a => a.Branch)
            .Include(a => a.Service)
            .Include(a => a.Customer)
            .Include(a => a.Consultant)
            .Where(a => a.CustomerId == customerId)
            .ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<Appointment>> GetByBranchIdWithIncludesAsync(int branchId)
    {
        return await _dbSet
            .Include(a => a.Branch)
            .Include(a => a.Service)
            .Include(a => a.Customer)
            .Include(a => a.Consultant)
            .Where(a => a.BranchId == branchId)
            .ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<Appointment>> GetAllWithIncludesAsync()
    {
        return await _dbSet
            .Include(a => a.Branch)
            .Include(a => a.Service)
            .Include(a => a.Customer)
            .Include(a => a.Consultant)
            .ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task<bool> ConfirmationCodeExistsAsync(string confirmationCode)
    {
        return await _dbSet.AnyAsync(a => a.ConfirmationCode == confirmationCode);
    }
}
