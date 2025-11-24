using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(IUserService userService, ILogger<AdminUsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of users with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResultDto<AdminUserDto>>> GetUsers([FromQuery] UserSearchDto search)
    {
        var result = await _userService.GetUsersAsync(search);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AdminUserDto>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    /// <summary>
    /// Update a user's details
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(int id, [FromBody] AdminUpdateUserDto dto)
    {
        var user = await _userService.UpdateUserAsync(id, dto);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    /// <summary>
    /// Activate a user account
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<ActionResult> ActivateUser(int id)
    {
        var result = await _userService.ActivateUserAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return Ok(new { message = "User activated successfully." });
    }

    /// <summary>
    /// Deactivate a user account
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult> DeactivateUser(int id)
    {
        var result = await _userService.DeactivateUserAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return Ok(new { message = "User deactivated successfully." });
    }

    /// <summary>
    /// Soft delete a user
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return Ok(new { message = "User deleted successfully." });
    }

    /// <summary>
    /// Change a user's role
    /// </summary>
    [HttpPut("{id}/role")]
    public async Task<ActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto dto)
    {
        var result = await _userService.UpdateUserRoleAsync(id, dto.Role);
        if (!result)
        {
            return NotFound();
        }
        return Ok(new { message = "User role updated successfully." });
    }

    /// <summary>
    /// Unlock a user's account
    /// </summary>
    [HttpPost("{id}/unlock")]
    public async Task<ActionResult> UnlockUser(int id)
    {
        var result = await _userService.UnlockUserAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return Ok(new { message = "User account unlocked successfully." });
    }

    /// <summary>
    /// Get user's activity log
    /// </summary>
    [HttpGet("{id}/activity")]
    public async Task<ActionResult<IEnumerable<LoginHistoryDto>>> GetUserActivity(int id, [FromQuery] int limit = 50)
    {
        var activity = await _userService.GetUserActivityAsync(id, limit);
        return Ok(activity);
    }
}
