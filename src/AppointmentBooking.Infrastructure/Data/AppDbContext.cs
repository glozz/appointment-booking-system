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
    }
}
