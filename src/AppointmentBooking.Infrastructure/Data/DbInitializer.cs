using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Infrastructure.Data;

/// <summary>
/// Database seeding utility - populates initial data
/// </summary>
public static class DbInitializer
{
    public static void Initialize(AppDbContext context, string? adminPassword = null)
    {
        // Ensure database is created
        context.Database.EnsureCreated();

        // Seed admin user if no users exist
        if (!context.Users.Any())
        {
            //if (!context.Users.Any(u => u.Email == "admin@appointmentbooking.com"))
            //{
            //    // Use provided password or default (should be changed in production via environment variable)
            //    var password = adminPassword ?? Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD") ?? "Admin@123";
         
            //    var adminUser = new User
            //    {
            //        FirstName = "Admin",
            //        LastName = "User",
            //        Email = "admin@appointmentbooking.com",
            //        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
            //        Phone = "+1234567890",
            //        Role = UserRole.Admin,
            //        IsActive = true,
            //        EmailVerified = true,
            //        CreatedAt = DateTime.UtcNow
            //    };
            //    context.Users.Add(adminUser);
            //    context.SaveChanges();
            //}
        }

        // Check if already seeded
        if (context.Services.Any())
        {
            // Ensure consultants are seeded even for existing databases
            SeedConsultants(context);
            return; // Database already seeded
        }

        //Seed Services (banking services)
        var services = new[]
        {
    new Service
    {
        Name = "New Account Application",
        Description = "Open a Capitec Global One account",
        DurationMinutes = 25,
        IsActive = true
    },
    new Service
    {
        Name = "Card Replacement",
        Description = "Replace a lost, stolen, or damaged Capitec card",
        DurationMinutes = 15,
        IsActive = true
    },
    new Service
    {
        Name = "Personal Loan Application",
        Description = "Apply for or enquire about a Capitec personal loan",
        DurationMinutes = 40,
        IsActive = true
    },
    new Service
    {
        Name = "Credit Limit Increase",
        Description = "Apply for a credit facility or adjust your credit limit",
        DurationMinutes = 30,
        IsActive = true
    },
    new Service
    {
        Name = "Bank Statement & Confirmation Letter",
        Description = "Request printed statements or bank confirmation letters",
        DurationMinutes = 10,
        IsActive = true
    },
    new Service
    {
        Name = "Biometric Verification",
        Description = "Perform fingerprint or facial verification services",
        DurationMinutes = 10,
        IsActive = true
    },
    new Service
    {
        Name = "Debit Order Dispute",
        Description = "Query or reverse an unauthorised debit order",
        DurationMinutes = 20,
        IsActive = true
    },
    new Service
    {
        Name = "FICA Document Update",
        Description = "Update address or identification documents",
        DurationMinutes = 15,
        IsActive = true
    },
    new Service
    {
        Name = "Account Enquiries",
        Description = "General enquiries about your Global One account",
        DurationMinutes = 15,
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
    Name = "Cape Town CBD Branch",
    Address = "54 Strand Street",
    City = "Cape Town",
    Phone = "+27214301000",
    Email = "capetowncbd@capitec.co.za",
    IsActive = true
},
new Branch
{
    Name = "Sandton City Branch",
    Address = "Rivonia Road, Sandton City Mall",
    City = "Johannesburg",
    Phone = "+27114601001",
    Email = "sandton@capitec.co.za",
    IsActive = true
},
new Branch
{
    Name = "Durban Gateway Branch",
    Address = "1 Palm Boulevard, Gateway Theatre of Shopping",
    City = "Durban",
    Phone = "+27315101002",
    Email = "gateway@capitec.co.za",
    IsActive = true
},
new Branch
{
    Name = "Pretoria Menlyn Branch",
    Address = "87 Atterbury Road, Menlyn Park",
    City = "Pretoria",
    Phone = "+27123201003",
    Email = "menlyn@capitec.co.za",
    IsActive = true
}
        };

        context.Branches.AddRange(branches);
        context.SaveChanges();

        // After SaveChanges, the branches array will have their generated IDs populated by EF Core

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

        // After SaveChanges, both branches and services arrays have their generated IDs populated by EF Core

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

        // Seed AppointmentTypes
        var appointmentTypes = new[]
        {
            new AppointmentType
            {
                Name = "Consultation",
                Description = "General consultation appointment",
                DurationMinutes = 30,
                Color = "#3B82F6",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new AppointmentType
            {
                Name = "Follow-up",
                Description = "Follow-up appointment",
                DurationMinutes = 15,
                Color = "#10B981",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new AppointmentType
            {
                Name = "Extended Consultation",
                Description = "Extended consultation for complex matters",
                DurationMinutes = 60,
                Color = "#8B5CF6",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new AppointmentType
            {
                Name = "Quick Service",
                Description = "Quick service for simple requests",
                DurationMinutes = 10,
                Color = "#F59E0B",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.AppointmentTypes.AddRange(appointmentTypes);
        context.SaveChanges();

        // Seed Consultants for all branches
        SeedConsultants(context);
    }

    /// <summary>
    /// Seeds 6 consultants per branch with unique South African first names and branch-specific surnames.
    /// This method is idempotent - it only adds consultants if fewer than 6 exist for a branch.
    /// </summary>
    public static void SeedConsultants(AppDbContext context)
    {
        // Quick check: if all branches already have 6+ consultants, skip seeding
        var branches = context.Branches.ToList();
        var consultantCountsByBranch = context.Consultants
            .GroupBy(c => c.BranchId)
            .ToDictionary(g => g.Key, g => g.Count());
        
        var allBranchesHaveEnoughConsultants = branches.All(b => 
            consultantCountsByBranch.TryGetValue(b.Id, out var count) && count >= 6);
        
        if (allBranchesHaveEnoughConsultants)
            return; // All branches already have enough consultants

        // South African first names for consultants (6 unique names per branch)
        var firstNames = new[] { "Thabo", "Sipho", "Nomsa", "Lerato", "Kabelo", "Ayanda" };

        foreach (var branch in branches)
        {
            // Count existing consultants for this branch
            var existingCount = consultantCountsByBranch.TryGetValue(branch.Id, out var cnt) ? cnt : 0;
            
            // Only seed if fewer than 6 consultants exist
            if (existingCount >= 6)
                continue;

            // Extract surname from branch name (e.g., "Sandton City Branch" -> "Sandton")
            // Use first word of branch name as surname
            var branchNameParts = branch.Name.Split(' ');
            var surname = branchNameParts.Length > 0 ? branchNameParts[0] : branch.Name;

            // Get existing consultant first names for this branch to avoid duplicates
            var existingFirstNames = context.Consultants
                .Where(c => c.BranchId == branch.Id)
                .Select(c => c.FirstName)
                .ToHashSet();

            foreach (var firstName in firstNames)
            {
                // Skip if this first name already exists for this branch
                if (existingFirstNames.Contains(firstName))
                    continue;

                // Stop if we've reached 6 consultants
                if (existingCount >= 6)
                    break;

                context.Consultants.Add(new Consultant
                {
                    FirstName = firstName,
                    LastName = surname,
                    BranchId = branch.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                existingCount++;
            }
        }

        context.SaveChanges();
    }
}
