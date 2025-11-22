using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
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
}

public record CancelAppointmentRequest(string Reason);
