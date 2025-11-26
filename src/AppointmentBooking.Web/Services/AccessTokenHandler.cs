using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AppointmentBooking.Web.Services;

/// <summary>
/// DelegatingHandler that attaches access_token cookie to outgoing HttpClient requests
/// and attempts token refresh via POST /api/auth/refresh on 401 responses.
/// </summary>
public class AccessTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccessTokenHandler> _logger;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AccessTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<AccessTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // Attach access token from cookie if available
        var accessToken = httpContext?.Request.Cookies["access_token"];
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If unauthorized and we have a refresh token, attempt refresh
        if (response.StatusCode == HttpStatusCode.Unauthorized && httpContext != null)
        {
            var refreshToken = httpContext.Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var refreshed = await TryRefreshTokenAsync(httpContext, refreshToken, cancellationToken);
                if (refreshed)
                {
                    // Retry the original request with the new token
                    var newAccessToken = httpContext.Request.Cookies["access_token"];
                    if (!string.IsNullOrEmpty(newAccessToken))
                    {
                        // Clone the request and retry
                        var retryRequest = await CloneRequestAsync(request);
                        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                        response = await base.SendAsync(retryRequest, cancellationToken);
                    }
                }
            }
        }

        return response;
    }

    private async Task<bool> TryRefreshTokenAsync(
        HttpContext httpContext,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        // Use semaphore to prevent multiple simultaneous refresh attempts
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Check if token was already refreshed by another request
            var currentRefreshToken = httpContext.Request.Cookies["refresh_token"];
            if (currentRefreshToken != refreshToken)
            {
                // Token was already refreshed
                return true;
            }

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
            var refreshEndpoint = $"{apiBaseUrl.TrimEnd('/')}/api/auth/refresh";

            using var refreshClient = new HttpClient();
            var refreshPayload = new { refreshToken };
            var content = new StringContent(
                JsonSerializer.Serialize(refreshPayload),
                Encoding.UTF8,
                "application/json");

            var refreshResponse = await refreshClient.PostAsync(refreshEndpoint, content, cancellationToken);

            if (refreshResponse.IsSuccessStatusCode)
            {
                var responseContent = await refreshResponse.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonSerializer.Deserialize<TokenRefreshResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    UpdateCookies(httpContext, tokenResponse);
                    _logger.LogInformation("Access token refreshed successfully");
                    return true;
                }
            }

            _logger.LogWarning("Failed to refresh access token. Status: {StatusCode}", refreshResponse.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing access token");
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void UpdateCookies(HttpContext httpContext, TokenRefreshResponse tokenResponse)
    {
        var accessTokenOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(tokenResponse.AccessTokenExpiryMinutes)
        };

        var refreshTokenOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(tokenResponse.RefreshTokenExpiryDays)
        };

        httpContext.Response.Cookies.Append("access_token", tokenResponse.AccessToken, accessTokenOptions);
        
        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
        {
            httpContext.Response.Cookies.Append("refresh_token", tokenResponse.RefreshToken, refreshTokenOptions);
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // Copy headers
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy content if present
        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);
            
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    private class TokenRefreshResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int AccessTokenExpiryMinutes { get; set; } = 60;
        public int RefreshTokenExpiryDays { get; set; } = 7;
    }
}
