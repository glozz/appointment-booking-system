using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Core.Interfaces;

/// <summary>
/// Specialized repository for Appointment entities with optimized queries including eager loading.
/// </summary>
public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>
    /// Gets an appointment by ID with all navigation properties eagerly loaded (Branch, Service, Customer, Consultant).
    /// </summary>
    Task<Appointment?> GetByIdWithIncludesAsync(int id);
    
    /// <summary>
    /// Gets an appointment by confirmation code with all navigation properties eagerly loaded.
    /// </summary>
    Task<Appointment?> GetByConfirmationCodeWithIncludesAsync(string confirmationCode);
    
    /// <summary>
    /// Gets all appointments for a customer with all navigation properties eagerly loaded.
    /// </summary>
    Task<IEnumerable<Appointment>> GetByCustomerIdWithIncludesAsync(int customerId);
    
    /// <summary>
    /// Gets all appointments for a branch with all navigation properties eagerly loaded.
    /// </summary>
    Task<IEnumerable<Appointment>> GetByBranchIdWithIncludesAsync(int branchId);
    
    /// <summary>
    /// Gets all appointments with all navigation properties eagerly loaded.
    /// </summary>
    Task<IEnumerable<Appointment>> GetAllWithIncludesAsync();
    
    /// <summary>
    /// Checks if a confirmation code already exists in the database.
    /// </summary>
    Task<bool> ConfirmationCodeExistsAsync(string confirmationCode);
}
