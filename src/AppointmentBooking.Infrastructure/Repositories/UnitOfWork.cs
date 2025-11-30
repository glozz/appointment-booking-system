using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;
using AppointmentBooking.Infrastructure.Data;

namespace AppointmentBooking.Infrastructure.Repositories;

/// <summary>
/// Unit of Work pattern implementation - coordinates repository operations
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        
        // Initialize all repositories
        Appointments = new Repository<Appointment>(_context);
        Branches = new Repository<Branch>(_context);
        Customers = new Repository<Customer>(_context);
        Services = new Repository<Service>(_context);
        BranchOperatingHours = new Repository<BranchOperatingHours>(_context);
        BranchServices = new Repository<BranchService>(_context);
        Users = new Repository<User>(_context);
        Sessions = new Repository<Session>(_context);
        ActivityLogs = new Repository<ActivityLog>(_context);
        Notifications = new Repository<Notification>(_context);
        AppointmentTypes = new Repository<AppointmentType>(_context);
        Consultants = new Repository<Consultant>(_context);
    }

    public IRepository<Appointment> Appointments { get; }
    public IRepository<Branch> Branches { get; }
    public IRepository<Customer> Customers { get; }
    public IRepository<Service> Services { get; }
    public IRepository<BranchOperatingHours> BranchOperatingHours { get; }
    public IRepository<BranchService> BranchServices { get; }
    public IRepository<User> Users { get; }
    public IRepository<Session> Sessions { get; }
    public IRepository<ActivityLog> ActivityLogs { get; }
    public IRepository<Notification> Notifications { get; }
    public IRepository<AppointmentType> AppointmentTypes { get; }
    public IRepository<Consultant> Consultants { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
