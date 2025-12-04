using System.Net.Http.Json;
using AppointmentBooking.Application.DTOs.Auth;
using Microsoft.AspNetCore.Identity.Data;

namespace AppointmentBooking.Web.Services.ApiClients;

/// <summary>
/// Implementation of authentication API client
/// </summary>
public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthApiClient> _logger;

    public AuthApiClient(HttpClient httpClient, ILogger<AuthApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Calling API login for: {Email}", request.Email);

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login API failed: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            _logger.LogInformation("Login API successful for: {Email}", request.Email);

            return authResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Cannot connect to API.  Is it running?");
            throw new InvalidOperationException("Unable to connect to authentication service. Please ensure the API is running.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling login API");
            throw;
        }
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Calling API register for: {Email}", request.Email);

            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Register API failed: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            _logger.LogInformation("Register API successful for: {Email}", request.Email);

            return authResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Cannot connect to API");
            throw new InvalidOperationException("Unable to connect to authentication service.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling register API");
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/auth/validate");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
    }
}