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
    public async Task<IActionResult> Dashboard(int? consultantId = null)
    {
        var actualConsultantId = await GetCurrentConsultantIdAsync(consultantId);
        
        // Admin without selection → show consultant picker
        if (actualConsultantId == null && User.IsInRole("Admin"))
        {
            var allConsultants = await _consultantService.GetAllConsultantsAsync();
            ViewBag.ReturnAction = "Dashboard";
            return View("SelectConsultant", allConsultants);
        }
        
        // Not authorized
        if (actualConsultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var consultant = await _consultantService.GetConsultantByIdAsync(actualConsultantId.Value);
        var todayAppointments = await _appointmentService.GetConsultantTodayAppointmentsAsync(actualConsultantId.Value);
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
        
        // Store for navigation and admin banner
        ViewBag.SelectedConsultantId = actualConsultantId.Value;
        ViewBag.IsAdmin = User.IsInRole("Admin");
        
        return View(viewModel);
    }

    /// <summary>
    /// Detailed view of today's schedule with timeline
    /// </summary>
    public async Task<IActionResult> TodaySchedule(int? consultantId = null)
    {
        var actualConsultantId = await GetCurrentConsultantIdAsync(consultantId);
        
        // Admin without selection → show consultant picker
        if (actualConsultantId == null && User.IsInRole("Admin"))
        {
            var allConsultants = await _consultantService.GetAllConsultantsAsync();
            ViewBag.ReturnAction = "TodaySchedule";
            return View("SelectConsultant", allConsultants);
        }
        
        // Not authorized
        if (actualConsultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var consultant = await _consultantService.GetConsultantByIdAsync(actualConsultantId.Value);
        var todayAppointments = await _appointmentService.GetConsultantTodayAppointmentsAsync(actualConsultantId.Value);
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
        
        // Store for navigation and admin banner
        ViewBag.SelectedConsultantId = actualConsultantId.Value;
        ViewBag.IsAdmin = User.IsInRole("Admin");
        
        return View(viewModel);
    }

    /// <summary>
    /// View upcoming appointments
    /// </summary>
    public async Task<IActionResult> Upcoming(int? consultantId = null)
    {
        var actualConsultantId = await GetCurrentConsultantIdAsync(consultantId);
        
        // Admin without selection → show consultant picker
        if (actualConsultantId == null && User.IsInRole("Admin"))
        {
            var allConsultants = await _consultantService.GetAllConsultantsAsync();
            ViewBag.ReturnAction = "Upcoming";
            return View("SelectConsultant", allConsultants);
        }
        
        // Not authorized
        if (actualConsultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var consultant = await _consultantService.GetConsultantByIdAsync(actualConsultantId.Value);
        var appointments = await _appointmentService.GetConsultantUpcomingAppointmentsAsync(actualConsultantId.Value);
        
        // Store for navigation and admin banner
        ViewBag.SelectedConsultantId = actualConsultantId.Value;
        ViewBag.IsAdmin = User.IsInRole("Admin");
        ViewBag.ConsultantName = consultant?.FullName ?? "Consultant";
        
        return View(appointments);
    }

    /// <summary>
    /// View past appointments
    /// </summary>
    public async Task<IActionResult> Past(int? consultantId = null)
    {
        var actualConsultantId = await GetCurrentConsultantIdAsync(consultantId);
        
        // Admin without selection → show consultant picker
        if (actualConsultantId == null && User.IsInRole("Admin"))
        {
            var allConsultants = await _consultantService.GetAllConsultantsAsync();
            ViewBag.ReturnAction = "Past";
            return View("SelectConsultant", allConsultants);
        }
        
        // Not authorized
        if (actualConsultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var consultant = await _consultantService.GetConsultantByIdAsync(actualConsultantId.Value);
        var appointments = await _appointmentService.GetConsultantPastAppointmentsAsync(actualConsultantId.Value);
        
        // Store for navigation and admin banner
        ViewBag.SelectedConsultantId = actualConsultantId.Value;
        ViewBag.IsAdmin = User.IsInRole("Admin");
        ViewBag.ConsultantName = consultant?.FullName ?? "Consultant";
        
        return View(appointments);
    }

    /// <summary>
    /// View schedule for a specific date
    /// </summary>
    public async Task<IActionResult> Schedule(DateTime? date = null, int? consultantId = null)
    {
        var actualConsultantId = await GetCurrentConsultantIdAsync(consultantId);
        
        // Admin without selection → show consultant picker
        if (actualConsultantId == null && User.IsInRole("Admin"))
        {
            var allConsultants = await _consultantService.GetAllConsultantsAsync();
            ViewBag.ReturnAction = "Schedule";
            ViewBag.ReturnDate = date;
            return View("SelectConsultant", allConsultants);
        }
        
        // Not authorized
        if (actualConsultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var consultant = await _consultantService.GetConsultantByIdAsync(actualConsultantId.Value);
        var scheduleDate = date ?? DateTime.Today;
        
        var appointments = await _appointmentService.GetConsultantAppointmentsByDateRangeAsync(
            actualConsultantId.Value, scheduleDate, scheduleDate.AddDays(1));
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
        // Store for navigation and admin banner
        ViewBag.SelectedConsultantId = actualConsultantId.Value;
        ViewBag.IsAdmin = User.IsInRole("Admin");
        
        return View(viewModel);
    }

    /// <summary>
    /// Update appointment status (Mark In Progress / Mark Complete)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, AppointmentStatus status, int? consultantId = null)
    {
        var actualConsultantId = await GetCurrentConsultantIdAsync(consultantId);
        if (actualConsultantId == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Verify the appointment belongs to the consultant being viewed
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }

        if (appointment.Consultant?.Id != actualConsultantId)
        {
            _logger.LogWarning("User attempted to update appointment {AppointmentId} belonging to consultant {AppointmentConsultantId} while viewing consultant {ViewingConsultantId}", 
                id, appointment.Consultant?.Id, actualConsultantId);
            return Forbid();
        }

        // Validate status transition
        if (status != AppointmentStatus.Completed && status != AppointmentStatus.Confirmed)
        {
            TempData["ErrorMessage"] = "Invalid status update.";
            return RedirectToAction(nameof(Dashboard), new { consultantId });
        }

        await _appointmentService.UpdateAppointmentStatusAsync(id, status);
        TempData["SuccessMessage"] = $"Appointment marked as {status}.";
        return RedirectToAction(nameof(Dashboard), new { consultantId });
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
    /// Get the current consultant's ID from user claims or use selected consultant for admins
    /// </summary>
    /// <param name="selectedConsultantId">Optional consultant ID selected by admin</param>
    private async Task<int?> GetCurrentConsultantIdAsync(int? selectedConsultantId = null)
    {
        // Get user ID from claims first (needed for both consultant and admin paths)
        var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
        
        // Security: Check if user is a Consultant (non-Admin) FIRST
        // Consultants can ONLY view their own schedules - ignore any consultantId parameter
        if (User.IsInRole("Consultant") && !User.IsInRole("Admin"))
        {
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var consultantUserId))
            {
                return null;
            }
            
            var consultant = await _consultantService.GetConsultantByUserIdAsync(consultantUserId);
            if (consultant == null)
            {
                return null;
            }
            
            // Log if consultant attempted to access another consultant's data (URL parameter tampering)
            if (selectedConsultantId.HasValue && selectedConsultantId.Value != consultant.Id)
            {
                _logger.LogWarning(
                    "Consultant (UserId: {UserId}, ConsultantId: {ConsultantId}) attempted unauthorized access to consultant {AttemptedConsultantId}",
                    consultantUserId, consultant.Id, selectedConsultantId.Value);
            }
            
            // ALWAYS return their own consultant ID - never allow access to other consultants
            return consultant.Id;
        }
        
        // For admin users with a selected consultant, validate and return that ID
        if (User.IsInRole("Admin") && selectedConsultantId.HasValue)
        {
            // Validate the consultant exists
            var selectedConsultant = await _consultantService.GetConsultantByIdAsync(selectedConsultantId.Value);
            if (selectedConsultant != null)
            {
                return selectedConsultantId.Value;
            }
            // Invalid consultant ID, return null to trigger selection page
            return null;
        }
        
        // For admin users without a selected consultant, return null to trigger selection page
        if (User.IsInRole("Admin"))
        {
            return null;
        }
        
        // Fallback for other users (shouldn't happen due to [Authorize] attribute, but be safe)
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        var fallbackConsultant = await _consultantService.GetConsultantByUserIdAsync(userId);
        return fallbackConsultant?.Id;
    }
}
