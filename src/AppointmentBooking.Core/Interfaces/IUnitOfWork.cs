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
    
    Task<int> SaveChangesAsync();
}
