using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's dashboard data
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<UserDashboardDto>> GetDashboard()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var dashboard = await _dashboardService.GetUserDashboardAsync(userId.Value);
        return Ok(dashboard);
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
