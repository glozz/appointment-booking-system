using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, IAuthService authService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var profile = await _userService.GetProfileAsync(userId.Value);
        if (profile == null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var profile = await _userService.UpdateProfileAsync(userId.Value, dto);
        if (profile == null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    [HttpPut("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _authService.ChangePasswordAsync(userId.Value, dto);
        if (!result)
        {
            return BadRequest(new { message = "Invalid current password or new password does not meet requirements." });
        }

        return Ok(new { message = "Password changed successfully." });
    }

    /// <summary>
    /// Get current user's active sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<IEnumerable<SessionDto>>> GetSessions()
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var sessions = await _userService.GetActiveSessionsAsync(userId.Value, sessionId);
        return Ok(sessions);
    }

    /// <summary>
    /// Revoke a specific session
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<ActionResult> RevokeSession(int sessionId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _userService.RevokeSessionAsync(userId.Value, sessionId);
        if (!result)
        {
            return NotFound();
        }

        return Ok(new { message = "Session revoked successfully." });
    }

    /// <summary>
    /// Revoke all sessions except current
    /// </summary>
    [HttpDelete("sessions")]
    public async Task<ActionResult> RevokeAllSessions()
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();
        if (userId == null)
        {
            return Unauthorized();
        }

        await _authService.RevokeAllSessionsAsync(userId.Value, sessionId);
        return Ok(new { message = "All other sessions revoked successfully." });
    }

    /// <summary>
    /// Get current user's login history
    /// </summary>
    [HttpGet("login-history")]
    public async Task<ActionResult<IEnumerable<LoginHistoryDto>>> GetLoginHistory([FromQuery] int limit = 20)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var history = await _userService.GetLoginHistoryAsync(userId.Value, limit);
        return Ok(history);
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private int GetSessionId()
    {
        var sessionIdClaim = User.FindFirst("sessionId")?.Value;
        return int.TryParse(sessionIdClaim, out var sessionId) ? sessionId : 0;
    }
}
