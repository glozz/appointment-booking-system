using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ApiClients;

/// <summary>
/// Interface for authentication API client
/// </summary>
public interface IAuthApiClient
{
    /// <summary>
    /// Login via API and receive JWT token
    /// </summary>
    Task<AuthResponseDto?> LoginAsync(LoginDto request);

    /// <summary>
    /// Register new user via API
    /// </summary>
    Task<AuthResponseDto?> RegisterAsync(RegisterDto request);

    /// <summary>
    /// Validate JWT token (uses token from current HttpContext)
    /// </summary>
    Task<bool> ValidateTokenAsync();
}
