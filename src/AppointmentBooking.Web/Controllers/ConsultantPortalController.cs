using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppointmentBooking.Application.Interfaces;

namespace AppointmentBooking.Web.Controllers;

/// <summary>
/// MVC Controller for consultant portal views
/// </summary>
[Authorize(Roles = "Consultant,Admin")]
public class ConsultantPortalController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly IConsultantService _consultantService;
    private readonly ILogger<ConsultantPortalController> _logger;

    public ConsultantPortalController(
        IAppointmentService appointmentService,
        IConsultantService consultantService,
        ILogger<ConsultantPortalController> logger)
    {
        _appointmentService = appointmentService;
        _consultantService = consultantService;
        _logger = logger;
    }

    /// <summary>
    /// Dashboard view showing today's schedule and upcoming appointments
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var consultantId = await GetCurrentConsultantIdAsync();
        if (consultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var today = await _appointmentService.GetConsultantTodayAppointmentsAsync(consultantId.Value);
        var upcoming = await _appointmentService.GetConsultantUpcomingAppointmentsAsync(consultantId.Value);
        
        ViewBag.TodayAppointments = today;
        ViewBag.UpcomingAppointments = upcoming;
        
        return View();
    }

    /// <summary>
    /// View today's appointments
    /// </summary>
    public async Task<IActionResult> Today()
    {
        var consultantId = await GetCurrentConsultantIdAsync();
        if (consultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var appointments = await _appointmentService.GetConsultantTodayAppointmentsAsync(consultantId.Value);
        return View(appointments);
    }

    /// <summary>
    /// View upcoming appointments
    /// </summary>
    public async Task<IActionResult> Upcoming()
    {
        var consultantId = await GetCurrentConsultantIdAsync();
        if (consultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var appointments = await _appointmentService.GetConsultantUpcomingAppointmentsAsync(consultantId.Value);
        return View(appointments);
    }

    /// <summary>
    /// View past appointments
    /// </summary>
    public async Task<IActionResult> Past()
    {
        var consultantId = await GetCurrentConsultantIdAsync();
        if (consultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var appointments = await _appointmentService.GetConsultantPastAppointmentsAsync(consultantId.Value);
        return View(appointments);
    }

    /// <summary>
    /// View schedule for a specific date
    /// </summary>
    public async Task<IActionResult> Schedule(DateTime? date)
    {
        var consultantId = await GetCurrentConsultantIdAsync();
        if (consultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var scheduleDate = date ?? DateTime.UtcNow.Date;
        var schedule = await _consultantService.GetConsultantScheduleAsync(consultantId.Value, scheduleDate);
        
        return View(schedule);
    }

    /// <summary>
    /// Get the current consultant's ID from user claims
    /// </summary>
    private async Task<int?> GetCurrentConsultantIdAsync()
    {
        // For admin users, they might be viewing as a specific consultant
        // For regular consultant users, get their linked consultant ID
        
        var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        var consultant = await _consultantService.GetConsultantByUserIdAsync(userId);
        return consultant?.Id;
    }
}
