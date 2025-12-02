using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Web.Models;

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
    /// Index redirects to Dashboard
    /// </summary>
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    /// <summary>
    /// Dashboard view showing today's schedule and upcoming appointments
    /// </summary>
    public async Task<IActionResult> Dashboard()
    {
        var consultantId = await GetCurrentConsultantIdAsync();
        if (consultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var consultant = await _consultantService.GetConsultantByIdAsync(consultantId.Value);
        var todayAppointments = await _appointmentService.GetConsultantTodayAppointmentsAsync(consultantId.Value);
        var todayList = todayAppointments.ToList();
        
        // Generate time slots for the day (08:00 - 17:00 with 30-minute intervals)
        var timeSlots = GenerateTimeSlots(todayList);
        
        var viewModel = new ConsultantDashboardViewModel
        {
            Consultant = consultant,
            Date = DateTime.Today,
            TodayAppointments = todayList,
            TotalToday = todayList.Count,
            CompletedToday = todayList.Count(a => a.Status == AppointmentStatus.Completed),
            RemainingToday = todayList.Count(a => a.Status != AppointmentStatus.Completed && a.Status != AppointmentStatus.Cancelled),
            TimeSlots = timeSlots
        };
        
        return View(viewModel);
    }

    /// <summary>
    /// Detailed view of today's schedule with timeline
    /// </summary>
    public async Task<IActionResult> TodaySchedule()
    {
        var consultantId = await GetCurrentConsultantIdAsync();
        if (consultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var consultant = await _consultantService.GetConsultantByIdAsync(consultantId.Value);
        var todayAppointments = await _appointmentService.GetConsultantTodayAppointmentsAsync(consultantId.Value);
        var todayList = todayAppointments.ToList();
        
        var timeSlots = GenerateTimeSlots(todayList);
        
        var viewModel = new ConsultantDashboardViewModel
        {
            Consultant = consultant,
            Date = DateTime.Today,
            TodayAppointments = todayList,
            TotalToday = todayList.Count,
            CompletedToday = todayList.Count(a => a.Status == AppointmentStatus.Completed),
            RemainingToday = todayList.Count(a => a.Status != AppointmentStatus.Completed && a.Status != AppointmentStatus.Cancelled),
            TimeSlots = timeSlots
        };
        
        return View(viewModel);
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

        var consultant = await _consultantService.GetConsultantByIdAsync(consultantId.Value);
        var scheduleDate = date ?? DateTime.Today;
        
        var appointments = await _appointmentService.GetConsultantAppointmentsByDateRangeAsync(
            consultantId.Value, scheduleDate, scheduleDate.AddDays(1));
        var appointmentList = appointments.ToList();
        
        var timeSlots = GenerateTimeSlots(appointmentList);
        
        var viewModel = new ConsultantDashboardViewModel
        {
            Consultant = consultant,
            Date = scheduleDate,
            TodayAppointments = appointmentList,
            TotalToday = appointmentList.Count,
            CompletedToday = appointmentList.Count(a => a.Status == AppointmentStatus.Completed),
            RemainingToday = appointmentList.Count(a => a.Status != AppointmentStatus.Completed && a.Status != AppointmentStatus.Cancelled),
            TimeSlots = timeSlots
        };
        
        ViewBag.SelectedDate = scheduleDate;
        return View(viewModel);
    }

    /// <summary>
    /// Update appointment status (Mark In Progress / Mark Complete)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, AppointmentStatus status)
    {
        var consultantId = await GetCurrentConsultantIdAsync();
        if (consultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Verify the consultant owns this appointment
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }

        if (appointment.Consultant?.Id != consultantId)
        {
            _logger.LogWarning("Consultant {ConsultantId} attempted to update appointment {AppointmentId} belonging to another consultant", 
                consultantId, id);
            return Forbid();
        }

        // Validate status transition
        if (status != AppointmentStatus.Completed && status != AppointmentStatus.Confirmed)
        {
            TempData["ErrorMessage"] = "Invalid status update.";
            return RedirectToAction(nameof(Dashboard));
        }

        await _appointmentService.UpdateAppointmentStatusAsync(id, status);
        TempData["SuccessMessage"] = $"Appointment marked as {status}.";
        return RedirectToAction(nameof(Dashboard));
    }

    /// <summary>
    /// Generate time slots for a day with appointment data
    /// </summary>
    private static IList<TimeSlotViewModel> GenerateTimeSlots(List<Application.DTOs.AppointmentDto> appointments)
    {
        var slots = new List<TimeSlotViewModel>();
        var startTime = new TimeSpan(8, 0, 0); // 08:00
        var endTime = new TimeSpan(17, 0, 0);  // 17:00
        var interval = TimeSpan.FromMinutes(30);
        
        for (var time = startTime; time < endTime; time += interval)
        {
            var appointment = appointments.FirstOrDefault(a => 
                a.StartTime <= time && a.EndTime > time && 
                a.Status != AppointmentStatus.Cancelled);
            
            slots.Add(new TimeSlotViewModel
            {
                Time = time,
                IsBooked = appointment != null,
                Appointment = appointment
            });
        }
        
        return slots;
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
