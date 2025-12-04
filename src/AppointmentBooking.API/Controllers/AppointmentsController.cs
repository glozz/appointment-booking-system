using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Exceptions;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(IAppointmentService appointmentService, ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        try
        {
            var appointment = await _appointmentService.CreateAppointmentAsync(dto);
            return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(int id)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        return appointment == null ? NotFound() : Ok(appointment);
    }

    [HttpGet("confirmation/{code}")]
    public async Task<ActionResult<AppointmentDto>> GetByConfirmationCode(string code)
    {
        var appointment = await _appointmentService.GetAppointmentByConfirmationCodeAsync(code);
        return appointment == null ? NotFound() : Ok(appointment);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAllAppointments()
    {
        var appointments = await _appointmentService.GetAllAppointmentsAsync();
        return Ok(appointments);
    }

    [HttpGet("customer/{email}")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetByCustomerEmail(string email)
    {
        var appointments = await _appointmentService.GetAppointmentsByCustomerEmailAsync(email);
        return Ok(appointments);
    }

    [HttpGet("branch/{branchId}")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetByBranch(int branchId)
    {
        var appointments = await _appointmentService.GetAppointmentsByBranchAsync(branchId);
        return Ok(appointments);
    }

    #region Customer Appointment Endpoints

    /// <summary>
    /// Get upcoming appointments for the current customer
    /// </summary>
    [HttpGet("my/upcoming")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetMyUpcomingAppointments()
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        var appointments = await _appointmentService.GetCustomerUpcomingAppointmentsAsync(email);
        return Ok(appointments);
    }

    /// <summary>
    /// Get past appointments for the current customer
    /// </summary>
    [HttpGet("my/past")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetMyPastAppointments()
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        var appointments = await _appointmentService.GetCustomerPastAppointmentsAsync(email);
        return Ok(appointments);
    }

    /// <summary>
    /// Get appointments for the current customer by status
    /// </summary>
    [HttpGet("my/status/{status}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetMyAppointmentsByStatus(AppointmentStatus status)
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        var appointments = await _appointmentService.GetCustomerAppointmentsByStatusAsync(email, status);
        return Ok(appointments);
    }

    /// <summary>
    /// Get appointments for the current customer within a date range
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetMyAppointments(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        if (startDate.HasValue && endDate.HasValue)
        {
            var appointments = await _appointmentService.GetCustomerAppointmentsByDateRangeAsync(email, startDate.Value, endDate.Value);
            return Ok(appointments);
        }

        // If no date range specified, return all appointments
        var allAppointments = await _appointmentService.GetAppointmentsByCustomerEmailAsync(email);
        return Ok(allAppointments);
    }

    #endregion

    [HttpPut("{id}")]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointment(int id, [FromBody] UpdateAppointmentDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { message = "ID mismatch" });

        try
        {
            var appointment = await _appointmentService.UpdateAppointmentAsync(dto);
            return Ok(appointment);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelAppointment(int id, [FromBody] CancelAppointmentRequest request)
    {
        try
        {
            await _appointmentService.CancelAppointmentAsync(id, request.Reason);
            return Ok(new { message = "Appointment cancelled successfully" });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update appointment status
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Consultant,Admin")]
    public async Task<ActionResult> UpdateAppointmentStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            await _appointmentService.UpdateAppointmentStatusAsync(id, request.Status);
            return Ok(new { message = "Appointment status updated successfully" });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
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

public record CancelAppointmentRequest(string Reason);
public record UpdateStatusRequest(AppointmentStatus Status);
