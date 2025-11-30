using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Appointment> Appointments { get; }
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
}
