using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentTypesController : ControllerBase
{
    private readonly IAppointmentTypeService _appointmentTypeService;
    private readonly ILogger<AppointmentTypesController> _logger;

    public AppointmentTypesController(IAppointmentTypeService appointmentTypeService, ILogger<AppointmentTypesController> logger)
    {
        _appointmentTypeService = appointmentTypeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active appointment types
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppointmentTypeDto>>> GetAll()
    {
        var types = await _appointmentTypeService.GetAllAsync();
        return Ok(types);
    }

    /// <summary>
    /// Get a specific appointment type
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentTypeDto>> GetById(int id)
    {
        var type = await _appointmentTypeService.GetByIdAsync(id);
        if (type == null)
        {
            return NotFound();
        }
        return Ok(type);
    }
}
