using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Web.ApiClients;
using AppointmentBooking.Web.Models;

namespace AppointmentBooking.Web.Controllers;

/// <summary>
/// MVC Controller for customer portal views
/// </summary>
[Authorize]
public class CustomerPortalController : Controller
{
    private readonly IApiAppointmentService _appointmentService;
    private readonly ILogger<CustomerPortalController> _logger;

    public CustomerPortalController(
        IApiAppointmentService appointmentService,
        ILogger<CustomerPortalController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    /// <summary>
    /// Index redirects to Dashboard
    /// </summary>
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    /// <summary>
    /// Dashboard view showing customer's appointments overview with tabs
    /// </summary>
    public async Task<IActionResult> Dashboard()
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", "Account");
        }

        var upcoming = await _appointmentService.GetCustomerUpcomingAppointmentsAsync(email);
        var past = await _appointmentService.GetCustomerPastAppointmentsAsync(email);
        
        var upcomingList = upcoming.ToList();
        var pastList = past.ToList();
        
        var viewModel = new CustomerDashboardViewModel
        {
            UpcomingAppointments = upcomingList,
            PastAppointments = pastList.Take(5), // Show only recent 5 past appointments
            TotalAppointments = upcomingList.Count + pastList.Count,
            UpcomingCount = upcomingList.Count,
            CompletedCount = pastList.Count(a => a.Status == AppointmentStatus.Completed)
        };
        
        return View(viewModel);
    }

    /// <summary>
    /// View appointment details
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", "Account");
        }

        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }

        // Verify the customer owns this appointment
        if (!string.Equals(appointment.Customer?.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("User {Email} attempted to access appointment {Id} belonging to another customer", email, id);
            return Forbid();
        }

        return View(appointment);
    }

    /// <summary>
    /// Cancel an appointment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string reason)
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", "Account");
        }

        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }

        // Verify the customer owns this appointment
        if (!string.Equals(appointment.Customer?.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("User {Email} attempted to cancel appointment {Id} belonging to another customer", email, id);
            return Forbid();
        }

        await _appointmentService.CancelAppointmentAsync(id, reason);
        TempData["SuccessMessage"] = "Appointment cancelled successfully.";
        return RedirectToAction(nameof(Dashboard));
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
