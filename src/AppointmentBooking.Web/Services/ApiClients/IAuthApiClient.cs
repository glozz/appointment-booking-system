using AppointmentBooking.Application.DTOs.Auth;

namespace AppointmentBooking.Web.Services.ApiClients;

/// <summary>
/// Client interface for authentication API operations
/// </summary>
public interface IAuthApiClient
{
    /// <summary>
    /// Authenticates a user and returns JWT token information
    /// </summary>
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    
    /// <summary>
    /// Registers a new user and returns JWT token information
    /// </summary>
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
}
