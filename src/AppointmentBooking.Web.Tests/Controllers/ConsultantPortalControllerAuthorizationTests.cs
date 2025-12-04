using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Web.ApiClients;
using AppointmentBooking.Web.Controllers;

namespace AppointmentBooking.Web.Tests.Controllers;

/// <summary>
/// Unit tests for ConsultantPortalController authorization logic.
/// Tests verify that consultants cannot access other consultants' schedules via URL parameter manipulation.
/// </summary>
public class ConsultantPortalControllerAuthorizationTests
{
    private readonly Mock<IApiAppointmentService> _appointmentServiceMock;
    private readonly Mock<IApiConsultantService> _consultantServiceMock;
    private readonly Mock<ILogger<ConsultantPortalController>> _loggerMock;

    public ConsultantPortalControllerAuthorizationTests()
    {
        _appointmentServiceMock = new Mock<IApiAppointmentService>();
        _consultantServiceMock = new Mock<IApiConsultantService>();
        _loggerMock = new Mock<ILogger<ConsultantPortalController>>();
    }

    [Fact]
    public async Task Dashboard_ConsultantAccessingOwnData_ReturnsOwnSchedule()
    {
        // Arrange
        var consultantId = 1;
        var userId = 10;
        var consultant = CreateConsultantDto(consultantId, "John", "Doe");
        
        _consultantServiceMock.Setup(s => s.GetConsultantByUserIdAsync(userId))
            .ReturnsAsync(consultant);
        _consultantServiceMock.Setup(s => s.GetConsultantByIdAsync(consultantId))
            .ReturnsAsync(consultant);
        _appointmentServiceMock.Setup(s => s.GetConsultantTodayAppointmentsAsync(consultantId))
            .ReturnsAsync(new List<AppointmentDto>());

        var controller = CreateController(userId, "Consultant", isAdmin: false);

        // Act - consultant accessing their own dashboard (no consultantId parameter)
        var result = await controller.Dashboard(null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
    }

    [Fact]
    public async Task Dashboard_ConsultantTryingToAccessOtherConsultant_ReturnsOwnSchedule()
    {
        // Arrange
        var ownConsultantId = 1;
        var otherConsultantId = 5;  // Trying to access this consultant's data
        var userId = 10;
        var ownConsultant = CreateConsultantDto(ownConsultantId, "John", "Doe");
        
        _consultantServiceMock.Setup(s => s.GetConsultantByUserIdAsync(userId))
            .ReturnsAsync(ownConsultant);
        _consultantServiceMock.Setup(s => s.GetConsultantByIdAsync(ownConsultantId))
            .ReturnsAsync(ownConsultant);
        _appointmentServiceMock.Setup(s => s.GetConsultantTodayAppointmentsAsync(ownConsultantId))
            .ReturnsAsync(new List<AppointmentDto>());

        var controller = CreateController(userId, "Consultant", isAdmin: false);

        // Act - consultant tries to access another consultant's data via URL parameter
        var result = await controller.Dashboard(otherConsultantId);

        // Assert - Should return OWN schedule, not the other consultant's
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
        
        // Verify that the other consultant's data was never fetched
        _consultantServiceMock.Verify(
            s => s.GetConsultantByIdAsync(otherConsultantId), 
            Times.Never, 
            "Should NOT fetch other consultant's data");
        
        // Verify own consultant data was fetched
        _consultantServiceMock.Verify(
            s => s.GetConsultantByIdAsync(ownConsultantId), 
            Times.Once, 
            "Should fetch OWN consultant data");
    }

    [Fact]
    public async Task Dashboard_ConsultantUnauthorizedAccessAttempt_LogsWarning()
    {
        // Arrange
        var ownConsultantId = 1;
        var otherConsultantId = 5;
        var userId = 10;
        var ownConsultant = CreateConsultantDto(ownConsultantId, "John", "Doe");
        
        _consultantServiceMock.Setup(s => s.GetConsultantByUserIdAsync(userId))
            .ReturnsAsync(ownConsultant);
        _consultantServiceMock.Setup(s => s.GetConsultantByIdAsync(ownConsultantId))
            .ReturnsAsync(ownConsultant);
        _appointmentServiceMock.Setup(s => s.GetConsultantTodayAppointmentsAsync(ownConsultantId))
            .ReturnsAsync(new List<AppointmentDto>());

        var controller = CreateController(userId, "Consultant", isAdmin: false);

        // Act - consultant tries unauthorized access
        await controller.Dashboard(otherConsultantId);

        // Assert - verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("unauthorized access")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Dashboard_AdminAccessingOtherConsultant_ReturnsRequestedSchedule()
    {
        // Arrange
        var requestedConsultantId = 5;
        var userId = 1;
        var requestedConsultant = CreateConsultantDto(requestedConsultantId, "Jane", "Smith");
        
        _consultantServiceMock.Setup(s => s.GetConsultantByIdAsync(requestedConsultantId))
            .ReturnsAsync(requestedConsultant);
        _appointmentServiceMock.Setup(s => s.GetConsultantTodayAppointmentsAsync(requestedConsultantId))
            .ReturnsAsync(new List<AppointmentDto>());

        var controller = CreateController(userId, "Admin", isAdmin: true);

        // Act - admin can access any consultant's data
        var result = await controller.Dashboard(requestedConsultantId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
        
        // Verify the requested consultant's data was fetched (called twice - once in authorization, once in action)
        _consultantServiceMock.Verify(
            s => s.GetConsultantByIdAsync(requestedConsultantId), 
            Times.AtLeastOnce);
        
        // Verify the appointments for the requested consultant were fetched
        _appointmentServiceMock.Verify(
            s => s.GetConsultantTodayAppointmentsAsync(requestedConsultantId),
            Times.Once);
    }

    [Fact]
    public async Task Dashboard_AdminWithoutConsultantId_ShowsConsultantPicker()
    {
        // Arrange
        var userId = 1;
        var allConsultants = new List<ConsultantDto>
        {
            CreateConsultantDto(1, "John", "Doe"),
            CreateConsultantDto(2, "Jane", "Smith")
        };
        
        _consultantServiceMock.Setup(s => s.GetAllConsultantsAsync())
            .ReturnsAsync(allConsultants);

        var controller = CreateController(userId, "Admin", isAdmin: true);

        // Act - admin without selecting a consultant
        var result = await controller.Dashboard(null);

        // Assert - should show consultant picker
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("SelectConsultant", viewResult.ViewName);
    }

    [Fact]
    public async Task TodaySchedule_ConsultantTryingToAccessOtherConsultant_ReturnsOwnSchedule()
    {
        // Arrange
        var ownConsultantId = 1;
        var otherConsultantId = 999;
        var userId = 10;
        var ownConsultant = CreateConsultantDto(ownConsultantId, "John", "Doe");
        
        _consultantServiceMock.Setup(s => s.GetConsultantByUserIdAsync(userId))
            .ReturnsAsync(ownConsultant);
        _consultantServiceMock.Setup(s => s.GetConsultantByIdAsync(ownConsultantId))
            .ReturnsAsync(ownConsultant);
        _appointmentServiceMock.Setup(s => s.GetConsultantTodayAppointmentsAsync(ownConsultantId))
            .ReturnsAsync(new List<AppointmentDto>());

        var controller = CreateController(userId, "Consultant", isAdmin: false);

        // Act - consultant tries to access via TodaySchedule endpoint
        var result = await controller.TodaySchedule(otherConsultantId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
        
        // Verify own consultant was used, not the attempted one
        _consultantServiceMock.Verify(
            s => s.GetConsultantByIdAsync(ownConsultantId), 
            Times.Once);
    }

    [Fact]
    public async Task Upcoming_ConsultantTryingToAccessOtherConsultant_ReturnsOwnSchedule()
    {
        // Arrange
        var ownConsultantId = 1;
        var otherConsultantId = 7;
        var userId = 10;
        var ownConsultant = CreateConsultantDto(ownConsultantId, "John", "Doe");
        
        _consultantServiceMock.Setup(s => s.GetConsultantByUserIdAsync(userId))
            .ReturnsAsync(ownConsultant);
        _consultantServiceMock.Setup(s => s.GetConsultantByIdAsync(ownConsultantId))
            .ReturnsAsync(ownConsultant);
        _appointmentServiceMock.Setup(s => s.GetConsultantUpcomingAppointmentsAsync(ownConsultantId))
            .ReturnsAsync(new List<AppointmentDto>());

        var controller = CreateController(userId, "Consultant", isAdmin: false);

        // Act
        var result = await controller.Upcoming(otherConsultantId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        
        // Verify own consultant data was used
        _appointmentServiceMock.Verify(
            s => s.GetConsultantUpcomingAppointmentsAsync(ownConsultantId), 
            Times.Once);
    }

    private ConsultantPortalController CreateController(int userId, string role, bool isAdmin)
    {
        var claims = new List<Claim>
        {
            new Claim("UserId", userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        
        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var controller = new ConsultantPortalController(
            _appointmentServiceMock.Object,
            _consultantServiceMock.Object,
            _loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    private static ConsultantDto CreateConsultantDto(int id, string firstName, string lastName)
    {
        return new ConsultantDto
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            BranchId = 1,
            BranchName = "Main Branch",
            IsActive = true
        };
    }
}
