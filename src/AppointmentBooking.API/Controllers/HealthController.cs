using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            status = "Healthy",
            time = DateTime.UtcNow
        });
    }
}
