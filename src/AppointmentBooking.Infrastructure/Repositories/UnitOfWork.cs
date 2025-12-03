using Microsoft.EntityFrameworkCore.Storage;
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
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        
        // Initialize all repositories
        Appointments = new AppointmentRepository(_context);
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

    public IAppointmentRepository Appointments { get; }
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
    
    /// <inheritdoc />
    /// <remarks>
    /// Nested transactions are not supported. If a transaction is already in progress,
    /// this will throw an InvalidOperationException. This is intentional to keep
    /// transaction handling simple and avoid complex rollback scenarios.
    /// </remarks>
    public async Task BeginTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress. Nested transactions are not supported.");
        }
        _currentTransaction = await _context.Database.BeginTransactionAsync();
    }
    
    /// <inheritdoc />
    public async Task CommitTransactionAsync()
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }
        
        try
        {
            await _context.SaveChangesAsync();
            await _currentTransaction.CommitAsync();
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
    
    /// <inheritdoc />
    public async Task RollbackTransactionAsync()
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }
        
        try
        {
            await _currentTransaction.RollbackAsync();
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
    
    /// <inheritdoc />
    public bool HasActiveTransaction()
    {
        return _currentTransaction != null;
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}
