using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(IDashboardService dashboardService, ILogger<AdminDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get admin dashboard statistics
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
    {
        var stats = await _dashboardService.GetAdminDashboardStatsAsync();
        return Ok(stats);
    }
}
