using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsultantsController : ControllerBase
{
    private readonly IConsultantService _consultantService;
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<ConsultantsController> _logger;

    public ConsultantsController(
        IConsultantService consultantService,
        IAppointmentService appointmentService,
        ILogger<ConsultantsController> logger)
    {
        _consultantService = consultantService;
        _appointmentService = appointmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all consultants
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConsultantDto>>> GetAllConsultants()
    {
        var consultants = await _consultantService.GetAllConsultantsAsync();
        return Ok(consultants);
    }

    /// <summary>
    /// Get consultant by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ConsultantDto>> GetConsultant(int id)
    {
        var consultant = await _consultantService.GetConsultantByIdAsync(id);
        return consultant == null ? NotFound() : Ok(consultant);
    }

    /// <summary>
    /// Get consultants by branch
    /// </summary>
    [HttpGet("branch/{branchId}")]
    public async Task<ActionResult<IEnumerable<ConsultantDto>>> GetConsultantsByBranch(int branchId)
    {
        var consultants = await _consultantService.GetConsultantsByBranchAsync(branchId);
        return Ok(consultants);
    }

    /// <summary>
    /// Get upcoming appointments for a consultant
    /// </summary>
    [HttpGet("{id}/appointments/upcoming")]
    [Authorize(Roles = "Consultant,Admin")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetUpcomingAppointments(int id)
    {
        // Authorization check: consultant can only view their own appointments
        if (!await IsAuthorizedForConsultant(id))
        {
            return Forbid();
        }

        var appointments = await _appointmentService.GetConsultantUpcomingAppointmentsAsync(id);
        return Ok(appointments);
    }

    /// <summary>
    /// Get past appointments for a consultant
    /// </summary>
    [HttpGet("{id}/appointments/past")]
    [Authorize(Roles = "Consultant,Admin")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetPastAppointments(int id)
    {
        if (!await IsAuthorizedForConsultant(id))
        {
            return Forbid();
        }

        var appointments = await _appointmentService.GetConsultantPastAppointmentsAsync(id);
        return Ok(appointments);
    }

    /// <summary>
    /// Get today's appointments for a consultant
    /// </summary>
    [HttpGet("{id}/appointments/today")]
    [Authorize(Roles = "Consultant,Admin")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetTodayAppointments(int id)
    {
        if (!await IsAuthorizedForConsultant(id))
        {
            return Forbid();
        }

        var appointments = await _appointmentService.GetConsultantTodayAppointmentsAsync(id);
        return Ok(appointments);
    }

    /// <summary>
    /// Get appointments for a consultant within a date range
    /// </summary>
    [HttpGet("{id}/appointments")]
    [Authorize(Roles = "Consultant,Admin")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointmentsByDateRange(
        int id, 
        [FromQuery] DateTime? startDate, 
        [FromQuery] DateTime? endDate)
    {
        if (!await IsAuthorizedForConsultant(id))
        {
            return Forbid();
        }

        if (startDate.HasValue && endDate.HasValue)
        {
            var appointments = await _appointmentService.GetConsultantAppointmentsByDateRangeAsync(id, startDate.Value, endDate.Value);
            return Ok(appointments);
        }

        // If no date range specified, return upcoming appointments
        var upcomingAppointments = await _appointmentService.GetConsultantUpcomingAppointmentsAsync(id);
        return Ok(upcomingAppointments);
    }

    /// <summary>
    /// Get a consultant's schedule for a specific date
    /// </summary>
    [HttpGet("{id}/schedule")]
    [Authorize(Roles = "Consultant,Admin")]
    public async Task<ActionResult<ConsultantScheduleDto>> GetSchedule(int id, [FromQuery] DateTime? date)
    {
        if (!await IsAuthorizedForConsultant(id))
        {
            return Forbid();
        }

        var scheduleDate = date ?? DateTime.UtcNow.Date;
        var schedule = await _consultantService.GetConsultantScheduleAsync(id, scheduleDate);
        return schedule == null ? NotFound() : Ok(schedule);
    }

    /// <summary>
    /// Check if the current user is authorized to access the specified consultant's data
    /// </summary>
    private async Task<bool> IsAuthorizedForConsultant(int consultantId)
    {
        // Admins can access any consultant's data
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        // Get the current user's ID from claims
        var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return false;
        }

        // Get the consultant associated with this user
        var consultant = await _consultantService.GetConsultantByUserIdAsync(userId);
        
        // Consultant can only access their own data
        return consultant?.Id == consultantId;
    }
}
