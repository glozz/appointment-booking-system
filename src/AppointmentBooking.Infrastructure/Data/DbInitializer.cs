using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Infrastructure.Data;

/// <summary>
/// Database seeding utility - populates initial data
/// </summary>
public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Ensure database is created
        context.Database.EnsureCreated();

        // Check if already seeded
        if (context.Services.Any())
        {
            return; // Database already seeded
        }

        // Seed Services (7 banking services)
        var services = new[]
        {
            new Service 
            { 
                Name = "Account Opening", 
                Description = "Open a new checking or savings account", 
                DurationMinutes = 30, 
                IsActive = true 
            },
            new Service 
            { 
                Name = "Loan Consultation", 
                Description = "Discuss personal or business loan options", 
                DurationMinutes = 45, 
                IsActive = true 
            },
            new Service 
            { 
                Name = "Credit Card Application", 
                Description = "Apply for a new credit card", 
                DurationMinutes = 30, 
                IsActive = true 
            },
            new Service 
            { 
                Name = "Mortgage Consultation", 
                Description = "Discuss home mortgage options and pre-approval", 
                DurationMinutes = 60, 
                IsActive = true 
            },
            new Service 
            { 
                Name = "Investment Advisory", 
                Description = "Financial planning and investment portfolio review", 
                DurationMinutes = 60, 
                IsActive = true 
            },
            new Service 
            { 
                Name = "Safe Deposit Box", 
                Description = "Rent or access your safe deposit box", 
                DurationMinutes = 15, 
                IsActive = true 
            },
            new Service 
            { 
                Name = "Foreign Exchange", 
                Description = "Currency exchange and international banking services", 
                DurationMinutes = 20, 
                IsActive = true 
            }
        };
        
        context.Services.AddRange(services);
        context.SaveChanges();

        // Seed Branches (4 locations in New York area)
        var branches = new[]
        {
            new Branch 
            { 
                Name = "Downtown Manhattan Branch", 
                Address = "123 Wall Street", 
                City = "New York", 
                Phone = "+12125551001", 
                Email = "downtown@appointmentbank.com", 
                IsActive = true 
            },
            new Branch 
            { 
                Name = "Uptown Manhattan Branch", 
                Address = "456 Park Avenue", 
                City = "New York", 
                Phone = "+12125551002", 
                Email = "uptown@appointmentbank.com", 
                IsActive = true 
            },
            new Branch 
            { 
                Name = "Brooklyn Heights Branch", 
                Address = "789 Atlantic Avenue", 
                City = "Brooklyn", 
                Phone = "+17185551003", 
                Email = "brooklyn@appointmentbank.com", 
                IsActive = true 
            },
            new Branch 
            { 
                Name = "Queens Center Branch", 
                Address = "321 Queens Boulevard", 
                City = "Queens", 
                Phone = "+17185551004", 
                Email = "queens@appointmentbank.com", 
                IsActive = true 
            }
        };
        
        context.Branches.AddRange(branches);
        context.SaveChanges();

        // Seed Operating Hours
        // Monday-Friday: 9:00 AM - 5:00 PM
        // Saturday: 9:00 AM - 1:00 PM
        // Sunday: Closed
        var operatingHours = new List<BranchOperatingHours>();
        
        foreach (var branch in branches)
        {
            // Monday (1) to Friday (5)
            for (int day = 1; day <= 5; day++)
            {
                operatingHours.Add(new BranchOperatingHours
                {
                    BranchId = branch.Id,
                    DayOfWeek = (DayOfWeek)day,
                    OpenTime = new TimeSpan(9, 0, 0),  // 9:00 AM
                    CloseTime = new TimeSpan(17, 0, 0), // 5:00 PM
                    IsClosed = false
                });
            }
            
            // Saturday (6)
            operatingHours.Add(new BranchOperatingHours
            {
                BranchId = branch.Id,
                DayOfWeek = DayOfWeek.Saturday,
                OpenTime = new TimeSpan(9, 0, 0),  // 9:00 AM
                CloseTime = new TimeSpan(13, 0, 0), // 1:00 PM
                IsClosed = false
            });
            
            // Sunday (0) - Closed
            operatingHours.Add(new BranchOperatingHours
            {
                BranchId = branch.Id,
                DayOfWeek = DayOfWeek.Sunday,
                OpenTime = new TimeSpan(0, 0, 0),
                CloseTime = new TimeSpan(0, 0, 0),
                IsClosed = true
            });
        }
        
        context.BranchOperatingHours.AddRange(operatingHours);
        context.SaveChanges();

        // Seed BranchServices (all branches offer all services)
        var branchServices = new List<BranchService>();
        
        foreach (var branch in branches)
        {
            foreach (var service in services)
            {
                branchServices.Add(new BranchService
                {
                    BranchId = branch.Id,
                    ServiceId = service.Id
                });
            }
        }
        
        context.BranchServices.AddRange(branchServices);
        context.SaveChanges();
    }
}
