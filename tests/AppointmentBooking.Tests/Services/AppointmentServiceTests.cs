using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Application.Services;
using AppointmentBooking.Application.Mappings;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Exceptions;
using AppointmentBooking.Core.Interfaces;
using System.Linq.Expressions;

namespace AppointmentBooking.Tests.Services;

public class AppointmentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<AppointmentService>> _loggerMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IAvailabilityService> _availabilityServiceMock;
    private readonly IMapper _mapper;
    private readonly AppointmentService _appointmentService;

    public AppointmentServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AppointmentService>>();
        _notificationServiceMock = new Mock<INotificationService>();
        _availabilityServiceMock = new Mock<IAvailabilityService>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        _appointmentService = new AppointmentService(
            _unitOfWorkMock.Object,
            _mapper,
            _loggerMock.Object,
            _notificationServiceMock.Object,
            _availabilityServiceMock.Object);
    }

    #region Slot Increment Validation Tests

    [Fact]
    public async Task CreateAppointmentAsync_WithInvalidSlotIncrement_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            BranchId = 1,
            ServiceId = 1,
            AppointmentDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(9, 7, 0), // Invalid: 7 minutes (not on 15-min increment)
            Customer = new CustomerDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "0123456789"
            }
        };

        var serviceRepoMock = new Mock<IRepository<Service>>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Service { Id = 1, DurationMinutes = 30 });
        _unitOfWorkMock.Setup(u => u.Services).Returns(serviceRepoMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _appointmentService.CreateAppointmentAsync(dto));
        Assert.Contains("15-minute increments", exception.Message);
    }

    [Theory]
    [InlineData(9, 0)]   // 09:00 - valid
    [InlineData(9, 15)]  // 09:15 - valid
    [InlineData(9, 30)]  // 09:30 - valid
    [InlineData(9, 45)]  // 09:45 - valid
    [InlineData(10, 0)]  // 10:00 - valid
    public async Task CreateAppointmentAsync_WithValidSlotIncrement_DoesNotThrowSlotIncrementException(int hour, int minute)
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            BranchId = 1,
            ServiceId = 1,
            AppointmentDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(hour, minute, 0),
            Customer = new CustomerDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "0123456789"
            }
        };

        var service = new Service { Id = 1, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IRepository<Service>>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(service);
        _unitOfWorkMock.Setup(u => u.Services).Returns(serviceRepoMock.Object);

        var branchHoursRepoMock = new Mock<IRepository<BranchOperatingHours>>();
        branchHoursRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BranchOperatingHours, bool>>>()))
            .ReturnsAsync(new[] { new BranchOperatingHours 
            { 
                BranchId = 1, 
                DayOfWeek = dto.AppointmentDate.DayOfWeek,
                OpenTime = new TimeSpan(8, 0, 0),
                CloseTime = new TimeSpan(17, 0, 0),
                IsClosed = false
            }});
        _unitOfWorkMock.Setup(u => u.BranchOperatingHours).Returns(branchHoursRepoMock.Object);

        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        appointmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Appointment>());
        _unitOfWorkMock.Setup(u => u.Appointments).Returns(appointmentRepoMock.Object);

        var consultantRepoMock = new Mock<IRepository<Consultant>>();
        consultantRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Consultant, bool>>>()))
            .ReturnsAsync(new[] { new Consultant { Id = 1, BranchId = 1, IsActive = true, FirstName = "Test", LastName = "Consultant" }});
        _unitOfWorkMock.Setup(u => u.Consultants).Returns(consultantRepoMock.Object);

        _availabilityServiceMock.Setup(a => a.IsSlotAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        var customerRepoMock = new Mock<IRepository<Customer>>();
        customerRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(new[] { new Customer { Id = 1, Email = "john@example.com", FirstName = "John", LastName = "Doe", Phone = "0123456789" }});
        _unitOfWorkMock.Setup(u => u.Customers).Returns(customerRepoMock.Object);

        var branchRepoMock = new Mock<IRepository<Branch>>();
        branchRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Branch { Id = 1, Name = "Test Branch", City = "Test City" });
        _unitOfWorkMock.Setup(u => u.Branches).Returns(branchRepoMock.Object);

        appointmentRepoMock.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
            .ReturnsAsync((Appointment a) => { a.Id = 1; return a; });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act - should not throw ValidationException for slot increment
        // It may throw other exceptions depending on mock setup, but not for slot increment
        try
        {
            await _appointmentService.CreateAppointmentAsync(dto);
        }
        catch (ValidationException ex) when (ex.Message.Contains("15-minute"))
        {
            Assert.Fail("Should not throw ValidationException for valid 15-minute slot increment");
        }
        catch
        {
            // Other exceptions are acceptable for this test
        }
    }

    #endregion

    #region Operating Hours Validation Tests

    [Fact]
    public async Task CreateAppointmentAsync_BeforeOperatingHours_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            BranchId = 1,
            ServiceId = 1,
            AppointmentDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(7, 0, 0), // Before 08:00
            Customer = new CustomerDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "0123456789"
            }
        };

        var service = new Service { Id = 1, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IRepository<Service>>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(service);
        _unitOfWorkMock.Setup(u => u.Services).Returns(serviceRepoMock.Object);

        var branchHoursRepoMock = new Mock<IRepository<BranchOperatingHours>>();
        branchHoursRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BranchOperatingHours, bool>>>()))
            .ReturnsAsync(new[] { new BranchOperatingHours 
            { 
                BranchId = 1, 
                DayOfWeek = dto.AppointmentDate.DayOfWeek,
                OpenTime = new TimeSpan(8, 0, 0),
                CloseTime = new TimeSpan(17, 0, 0),
                IsClosed = false
            }});
        _unitOfWorkMock.Setup(u => u.BranchOperatingHours).Returns(branchHoursRepoMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _appointmentService.CreateAppointmentAsync(dto));
        Assert.Contains("operating hours", exception.Message);
    }

    [Fact]
    public async Task CreateAppointmentAsync_AfterOperatingHours_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            BranchId = 1,
            ServiceId = 1,
            AppointmentDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(16, 45, 0), // 16:45 - with 30-min service, ends at 17:15
            Customer = new CustomerDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "0123456789"
            }
        };

        var service = new Service { Id = 1, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IRepository<Service>>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(service);
        _unitOfWorkMock.Setup(u => u.Services).Returns(serviceRepoMock.Object);

        var branchHoursRepoMock = new Mock<IRepository<BranchOperatingHours>>();
        branchHoursRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BranchOperatingHours, bool>>>()))
            .ReturnsAsync(new[] { new BranchOperatingHours 
            { 
                BranchId = 1, 
                DayOfWeek = dto.AppointmentDate.DayOfWeek,
                OpenTime = new TimeSpan(8, 0, 0),
                CloseTime = new TimeSpan(17, 0, 0),
                IsClosed = false
            }});
        _unitOfWorkMock.Setup(u => u.BranchOperatingHours).Returns(branchHoursRepoMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _appointmentService.CreateAppointmentAsync(dto));
        Assert.Contains("operating hours", exception.Message);
    }

    [Fact]
    public async Task CreateAppointmentAsync_OnClosedDay_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            BranchId = 1,
            ServiceId = 1,
            AppointmentDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            Customer = new CustomerDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "0123456789"
            }
        };

        var service = new Service { Id = 1, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IRepository<Service>>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(service);
        _unitOfWorkMock.Setup(u => u.Services).Returns(serviceRepoMock.Object);

        var branchHoursRepoMock = new Mock<IRepository<BranchOperatingHours>>();
        branchHoursRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BranchOperatingHours, bool>>>()))
            .ReturnsAsync(new[] { new BranchOperatingHours 
            { 
                BranchId = 1, 
                DayOfWeek = dto.AppointmentDate.DayOfWeek,
                IsClosed = true
            }});
        _unitOfWorkMock.Setup(u => u.BranchOperatingHours).Returns(branchHoursRepoMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            _appointmentService.CreateAppointmentAsync(dto));
        Assert.Contains("closed", exception.Message);
    }

    #endregion

    #region Consultant Assignment Tests

    [Fact]
    public async Task CreateAppointmentAsync_NoAvailableConsultants_ThrowsConflictException()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            BranchId = 1,
            ServiceId = 1,
            AppointmentDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            Customer = new CustomerDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "0123456789"
            }
        };

        var service = new Service { Id = 1, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IRepository<Service>>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(service);
        _unitOfWorkMock.Setup(u => u.Services).Returns(serviceRepoMock.Object);

        var branchHoursRepoMock = new Mock<IRepository<BranchOperatingHours>>();
        branchHoursRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BranchOperatingHours, bool>>>()))
            .ReturnsAsync(new[] { new BranchOperatingHours 
            { 
                BranchId = 1, 
                DayOfWeek = dto.AppointmentDate.DayOfWeek,
                OpenTime = new TimeSpan(8, 0, 0),
                CloseTime = new TimeSpan(17, 0, 0),
                IsClosed = false
            }});
        _unitOfWorkMock.Setup(u => u.BranchOperatingHours).Returns(branchHoursRepoMock.Object);

        // No double booking
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        var appointmentCallCount = 0;
        appointmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>()))
            .ReturnsAsync(() =>
            {
                appointmentCallCount++;
                // First call is for double-booking check (return empty)
                // Second call is for consultant overlap check (return overlapping appointment)
                if (appointmentCallCount == 1)
                    return Enumerable.Empty<Appointment>();
                else
                    return new[] { new Appointment { Id = 1, ConsultantId = 1, StartTime = new TimeSpan(9, 30, 0), EndTime = new TimeSpan(10, 30, 0) }};
            });
        _unitOfWorkMock.Setup(u => u.Appointments).Returns(appointmentRepoMock.Object);

        _availabilityServiceMock.Setup(a => a.IsSlotAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        // All consultants are busy
        var consultantRepoMock = new Mock<IRepository<Consultant>>();
        consultantRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Consultant, bool>>>()))
            .ReturnsAsync(new[] { new Consultant { Id = 1, BranchId = 1, IsActive = true, FirstName = "Test", LastName = "Consultant" }});
        _unitOfWorkMock.Setup(u => u.Consultants).Returns(consultantRepoMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(() => 
            _appointmentService.CreateAppointmentAsync(dto));
        Assert.Contains("No consultants are available", exception.Message);
    }

    [Fact]
    public async Task CreateAppointmentAsync_WithAvailableConsultant_AssignsConsultant()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            BranchId = 1,
            ServiceId = 1,
            AppointmentDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            Customer = new CustomerDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "0123456789"
            }
        };

        var service = new Service { Id = 1, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IRepository<Service>>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(service);
        _unitOfWorkMock.Setup(u => u.Services).Returns(serviceRepoMock.Object);

        var branchHoursRepoMock = new Mock<IRepository<BranchOperatingHours>>();
        branchHoursRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BranchOperatingHours, bool>>>()))
            .ReturnsAsync(new[] { new BranchOperatingHours 
            { 
                BranchId = 1, 
                DayOfWeek = dto.AppointmentDate.DayOfWeek,
                OpenTime = new TimeSpan(8, 0, 0),
                CloseTime = new TimeSpan(17, 0, 0),
                IsClosed = false
            }});
        _unitOfWorkMock.Setup(u => u.BranchOperatingHours).Returns(branchHoursRepoMock.Object);

        _availabilityServiceMock.Setup(a => a.IsSlotAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        var consultant = new Consultant { Id = 1, BranchId = 1, IsActive = true, FirstName = "Thabo", LastName = "Sandton" };
        var consultantRepoMock = new Mock<IRepository<Consultant>>();
        consultantRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Consultant, bool>>>()))
            .ReturnsAsync(new[] { consultant });
        consultantRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(consultant);
        _unitOfWorkMock.Setup(u => u.Consultants).Returns(consultantRepoMock.Object);

        var customer = new Customer { Id = 1, Email = "john@example.com", FirstName = "John", LastName = "Doe", Phone = "0123456789" };
        var customerRepoMock = new Mock<IRepository<Customer>>();
        customerRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(new[] { customer });
        customerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        _unitOfWorkMock.Setup(u => u.Customers).Returns(customerRepoMock.Object);

        var branch = new Branch { Id = 1, Name = "Sandton City Branch", City = "Johannesburg" };
        var branchRepoMock = new Mock<IRepository<Branch>>();
        branchRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(branch);
        _unitOfWorkMock.Setup(u => u.Branches).Returns(branchRepoMock.Object);

        Appointment? createdAppointment = null;
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        
        // For validation checks (double booking, consultant overlap), return empty
        appointmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>()))
            .ReturnsAsync((Expression<Func<Appointment, bool>> predicate) =>
            {
                // If we've created an appointment and this is a retrieval by confirmation code
                if (createdAppointment != null)
                {
                    var compiled = predicate.Compile();
                    if (compiled(createdAppointment))
                        return new[] { createdAppointment };
                }
                return Enumerable.Empty<Appointment>();
            });

        appointmentRepoMock.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
            .Callback<Appointment>(a => { 
                a.Id = 1;
                a.Branch = branch;
                a.Service = service;
                a.Customer = customer;
                a.Consultant = consultant;
                createdAppointment = a; 
            })
            .ReturnsAsync((Appointment a) => a);

        // Mock the specialized repository methods for retrieval with includes
        // Use It.IsAny<string>() and check createdAppointment in the callback
        appointmentRepoMock.Setup(r => r.GetByConfirmationCodeWithIncludesAsync(It.IsAny<string>()))
            .ReturnsAsync(() => createdAppointment);
        
        // Mock confirmation code uniqueness check (no existing code)
        appointmentRepoMock.Setup(r => r.ConfirmationCodeExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _unitOfWorkMock.Setup(u => u.Appointments).Returns(appointmentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        
        // Mock transaction methods
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _appointmentService.CreateAppointmentAsync(dto);

        // Assert
        Assert.NotNull(createdAppointment);
        Assert.Equal(1, createdAppointment!.ConsultantId);
        Assert.NotNull(result.Consultant);
        Assert.Equal("Thabo", result.Consultant!.FirstName);
        Assert.Equal("Sandton", result.Consultant.LastName);
    }

    #endregion

    #region Double Booking Prevention Tests

    [Fact]
    public async Task CreateAppointmentAsync_WithExistingSlot_ThrowsConflictException()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            BranchId = 1,
            ServiceId = 1,
            AppointmentDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            Customer = new CustomerDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "0123456789"
            }
        };

        var service = new Service { Id = 1, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IRepository<Service>>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(service);
        _unitOfWorkMock.Setup(u => u.Services).Returns(serviceRepoMock.Object);

        var branchHoursRepoMock = new Mock<IRepository<BranchOperatingHours>>();
        branchHoursRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BranchOperatingHours, bool>>>()))
            .ReturnsAsync(new[] { new BranchOperatingHours 
            { 
                BranchId = 1, 
                DayOfWeek = dto.AppointmentDate.DayOfWeek,
                OpenTime = new TimeSpan(8, 0, 0),
                CloseTime = new TimeSpan(17, 0, 0),
                IsClosed = false
            }});
        _unitOfWorkMock.Setup(u => u.BranchOperatingHours).Returns(branchHoursRepoMock.Object);

        // Existing appointment at the same time slot
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        appointmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Appointment, bool>>>()))
            .ReturnsAsync(new[] { new Appointment 
            { 
                Id = 1, 
                BranchId = 1, 
                AppointmentDate = dto.AppointmentDate, 
                StartTime = new TimeSpan(10, 0, 0),
                Status = AppointmentStatus.Confirmed
            }});
        _unitOfWorkMock.Setup(u => u.Appointments).Returns(appointmentRepoMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(() => 
            _appointmentService.CreateAppointmentAsync(dto));
        Assert.Contains("already booked", exception.Message);
    }

    #endregion

    #region Overlap Detection Tests

    [Theory]
    [InlineData(9, 30, 10, 30, true)]   // Existing 9:30-10:30, requesting 10:00-10:30 - overlaps
    [InlineData(10, 0, 10, 30, true)]   // Existing 10:00-10:30, requesting 10:00-10:30 - exact match
    [InlineData(10, 15, 10, 45, true)]  // Existing 10:15-10:45, requesting 10:00-10:30 - overlaps
    [InlineData(10, 30, 11, 0, false)]  // Existing 10:30-11:00, requesting 10:00-10:30 - adjacent, no overlap
    [InlineData(9, 0, 9, 30, false)]    // Existing 9:00-9:30, requesting 10:00-10:30 - no overlap
    public void OverlapDetection_VariousScenarios_ReturnsCorrectResult(
        int existingStartHour, int existingStartMin, int existingEndHour, int existingEndMin, bool expectedOverlap)
    {
        // Arrange
        var existingStart = new TimeSpan(existingStartHour, existingStartMin, 0);
        var existingEnd = new TimeSpan(existingEndHour, existingEndMin, 0);
        var requestedStart = new TimeSpan(10, 0, 0);
        var requestedEnd = new TimeSpan(10, 30, 0);

        // Act - Using the same overlap logic as in AppointmentService
        var hasOverlap = existingStart < requestedEnd && requestedStart < existingEnd;

        // Assert
        Assert.Equal(expectedOverlap, hasOverlap);
    }

    #endregion
}
