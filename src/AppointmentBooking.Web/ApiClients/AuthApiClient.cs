using System.Net.Http.Json;
using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ApiClients;

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

    public async Task<AuthResponseDto?> LoginAsync(LoginDto request)
    {
        try
        {
            _logger.LogInformation("Calling API login endpoint for: {Email}", request.Email);
            
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login API call failed: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            _logger.LogInformation("Login API call successful for: {Email}", request.Email);
            
            return authResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling login API");
            return null;
        }
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto request)
    {
        try
        {
            _logger.LogInformation("Calling API register endpoint for: {Email}", request.Email);
            
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Register API call failed: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            _logger.LogInformation("Register API call successful for: {Email}", request.Email);
            
            return authResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling register API");
            return null;
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
