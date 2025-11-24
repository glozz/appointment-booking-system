using Microsoft.EntityFrameworkCore;
using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<BranchOperatingHours> BranchOperatingHours => Set<BranchOperatingHours>();
    public DbSet<BranchService> BranchServices => Set<BranchService>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure BranchService many-to-many
        modelBuilder.Entity<BranchService>()
            .HasKey(bs => new { bs.BranchId, bs.ServiceId });

        modelBuilder.Entity<BranchService>()
            .HasOne(bs => bs.Branch)
            .WithMany(b => b.BranchServices)
            .HasForeignKey(bs => bs.BranchId);

        modelBuilder.Entity<BranchService>()
            .HasOne(bs => bs.Service)
            .WithMany(s => s.BranchServices)
            .HasForeignKey(bs => bs.ServiceId);

        // Unique indexes
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => a.ConfirmationCode)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<Branch>()
            .HasIndex(b => b.Email)
            .IsUnique();

        // User configuration
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.EmailVerificationToken);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.PasswordResetToken);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);

        // Session configuration
        modelBuilder.Entity<Session>()
            .HasIndex(s => s.RefreshToken);

        modelBuilder.Entity<Session>()
            .HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ActivityLog configuration
        modelBuilder.Entity<ActivityLog>()
            .HasIndex(a => a.UserId);

        modelBuilder.Entity<ActivityLog>()
            .HasIndex(a => a.CreatedAt);

        modelBuilder.Entity<ActivityLog>()
            .HasOne(a => a.User)
            .WithMany(u => u.ActivityLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Notification configuration
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead });

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
