using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AppointmentBooking.Application.DTOs.Auth;

namespace AppointmentBooking.Web.Services.ApiClients;

/// <summary>
/// HTTP client implementation for authentication API operations
/// </summary>
public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthApiClient(HttpClient httpClient, ILogger<AuthApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Calling API login endpoint for {Email}", request.Email);

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, JsonOptions);
                _logger.LogInformation("Login successful for {Email}", request.Email);
                return authResponse;
            }

            _logger.LogWarning("Login failed for {Email}. Status: {StatusCode}", request.Email, response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during login for {Email}", request.Email);
            throw new InvalidOperationException("Unable to connect to authentication service", ex);
        }
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Calling API register endpoint for {Email}", request.Email);

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/auth/register", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, JsonOptions);
                _logger.LogInformation("Registration successful for {Email}", request.Email);
                return authResponse;
            }

            _logger.LogWarning("Registration failed for {Email}. Status: {StatusCode}", request.Email, response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during registration for {Email}", request.Email);
            throw new InvalidOperationException("Unable to connect to authentication service", ex);
        }
    }
}
