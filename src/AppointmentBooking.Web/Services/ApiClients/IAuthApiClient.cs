using AppointmentBooking.Application.DTOs.Auth;
using Microsoft.AspNetCore.Identity.Data;

namespace AppointmentBooking.Web.Services.ApiClients;

/// <summary>
/// API client for authentication operations
/// </summary>
public interface IAuthApiClient
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<bool> ValidateTokenAsync();
}