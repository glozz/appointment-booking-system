using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentBooking.Infrastructure.Data;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AppDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetHealth()
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync();
            
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                services = new
                {
                    database = "Connected",
                    api = "Running"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = "Database connection failed"
            });
        }
    }
}
