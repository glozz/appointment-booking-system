using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Specialized appointment repository with optimized queries for eager loading.
    /// </summary>
    IAppointmentRepository Appointments { get; }
    IRepository<Branch> Branches { get; }
    IRepository<Customer> Customers { get; }
    IRepository<Service> Services { get; }
    IRepository<BranchOperatingHours> BranchOperatingHours { get; }
    IRepository<BranchService> BranchServices { get; }
    IRepository<User> Users { get; }
    IRepository<Session> Sessions { get; }
    IRepository<ActivityLog> ActivityLogs { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<AppointmentType> AppointmentTypes { get; }
    IRepository<Consultant> Consultants { get; }
    
    Task<int> SaveChangesAsync();
    
    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    Task BeginTransactionAsync();
    
    /// <summary>
    /// Commits the current database transaction.
    /// </summary>
    Task CommitTransactionAsync();
    
    /// <summary>
    /// Rolls back the current database transaction.
    /// </summary>
    Task RollbackTransactionAsync();
    
    /// <summary>
    /// Checks if there is an active database transaction.
    /// </summary>
    bool HasActiveTransaction();
}
