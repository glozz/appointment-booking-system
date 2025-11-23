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
    }
}
