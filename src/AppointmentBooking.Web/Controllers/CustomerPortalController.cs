using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Web.Controllers;

/// <summary>
/// MVC Controller for customer portal views
/// </summary>
[Authorize]
public class CustomerPortalController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<CustomerPortalController> _logger;

    public CustomerPortalController(
        IAppointmentService appointmentService,
        ILogger<CustomerPortalController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    /// <summary>
    /// Dashboard view showing customer's appointments overview
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", "Account");
        }

        var upcoming = await _appointmentService.GetCustomerUpcomingAppointmentsAsync(email);
        var past = await _appointmentService.GetCustomerPastAppointmentsAsync(email);
        
        ViewBag.UpcomingAppointments = upcoming;
        ViewBag.PastAppointments = past.Take(5); // Show only recent 5 past appointments
        
        return View();
    }

    /// <summary>
    /// View all upcoming appointments
    /// </summary>
    public async Task<IActionResult> Upcoming()
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", "Account");
        }

        var appointments = await _appointmentService.GetCustomerUpcomingAppointmentsAsync(email);
        return View(appointments);
    }

    /// <summary>
    /// View past appointments
    /// </summary>
    public async Task<IActionResult> Past()
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", "Account");
        }

        var appointments = await _appointmentService.GetCustomerPastAppointmentsAsync(email);
        return View(appointments);
    }

    /// <summary>
    /// View appointments by status
    /// </summary>
    public async Task<IActionResult> ByStatus(AppointmentStatus status)
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", "Account");
        }

        var appointments = await _appointmentService.GetCustomerAppointmentsByStatusAsync(email, status);
        ViewBag.Status = status;
        return View(appointments);
    }

    /// <summary>
    /// View appointments within a date range
    /// </summary>
    public async Task<IActionResult> ByDateRange(DateTime? startDate, DateTime? endDate)
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", "Account");
        }

        var start = startDate ?? DateTime.UtcNow.Date;
        var end = endDate ?? start.AddMonths(1);

        var appointments = await _appointmentService.GetCustomerAppointmentsByDateRangeAsync(email, start, end);
        ViewBag.StartDate = start;
        ViewBag.EndDate = end;
        return View(appointments);
    }

    /// <summary>
    /// Get the current user's email from claims
    /// </summary>
    private string? GetCurrentUserEmail()
    {
        return User.FindFirst("email")?.Value ?? 
               User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    }
}
