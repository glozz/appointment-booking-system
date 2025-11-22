using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceService _serviceService;

    public ServicesController(IServiceService serviceService)
    {
        _serviceService = serviceService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetAllServices()
    {
        var services = await _serviceService.GetAllServicesAsync();
        return Ok(services);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetActiveServices()
    {
        var services = await _serviceService.GetActiveServicesAsync();
        return Ok(services);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceDto>> GetService(int id)
    {
        var service = await _serviceService.GetServiceByIdAsync(id);
        return service == null ? NotFound() : Ok(service);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceDto>> CreateService([FromBody] ServiceDto dto)
    {
        var service = await _serviceService.CreateServiceAsync(dto);
        return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
    }
}
