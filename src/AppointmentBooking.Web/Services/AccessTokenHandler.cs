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
    private readonly IWebHostEnvironment _environment;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static string? _lastRefreshedAccessToken;
    private static string? _lastRefreshedFromRefreshToken;

    public AccessTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<AccessTokenHandler> logger,
        IWebHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
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

        // Clone content before first send to preserve it for potential retry
        byte[]? contentBytes = null;
        if (request.Content != null)
        {
            contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If unauthorized and we have a refresh token, attempt refresh
        if (response.StatusCode == HttpStatusCode.Unauthorized && httpContext != null)
        {
            var refreshToken = httpContext.Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var refreshResult = await TryRefreshTokenAsync(httpContext, refreshToken, cancellationToken);
                if (refreshResult.Success && !string.IsNullOrEmpty(refreshResult.NewAccessToken))
                {
                    // Clone the request and retry with the new token (from refresh result, not from cookies)
                    var retryRequest = CloneRequest(request, contentBytes);
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshResult.NewAccessToken);
                    response = await base.SendAsync(retryRequest, cancellationToken);
                }
            }
        }

        return response;
    }

    private async Task<TokenRefreshResult> TryRefreshTokenAsync(
        HttpContext httpContext,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        // Use semaphore to prevent multiple simultaneous refresh attempts
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Check if token was already refreshed by another request (using stored value, not cookies)
            if (_lastRefreshedFromRefreshToken == refreshToken && !string.IsNullOrEmpty(_lastRefreshedAccessToken))
            {
                // Token was already refreshed, return the stored access token
                _logger.LogDebug("Using previously refreshed token");
                return new TokenRefreshResult { Success = true, NewAccessToken = _lastRefreshedAccessToken };
            }

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:61577";
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
                    
                    // Store the refreshed token for concurrent requests
                    _lastRefreshedFromRefreshToken = refreshToken;
                    _lastRefreshedAccessToken = tokenResponse.AccessToken;
                    
                    _logger.LogInformation("Access token refreshed successfully");
                    return new TokenRefreshResult { Success = true, NewAccessToken = tokenResponse.AccessToken };
                }
            }

            _logger.LogWarning("Failed to refresh access token. Status: {StatusCode}", refreshResponse.StatusCode);
            return new TokenRefreshResult { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing access token");
            return new TokenRefreshResult { Success = false };
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void UpdateCookies(HttpContext httpContext, TokenRefreshResponse tokenResponse)
    {
        // Use secure cookies in production, allow non-secure in development
        var useSecureCookies = !_environment.IsDevelopment();
        
        var accessTokenOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = useSecureCookies,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(tokenResponse.AccessTokenExpiryMinutes)
        };

        var refreshTokenOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = useSecureCookies,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(tokenResponse.RefreshTokenExpiryDays)
        };

        httpContext.Response.Cookies.Append("access_token", tokenResponse.AccessToken, accessTokenOptions);
        
        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
        {
            httpContext.Response.Cookies.Append("refresh_token", tokenResponse.RefreshToken, refreshTokenOptions);
        }
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request, byte[]? contentBytes)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // Copy headers (except Authorization which will be set by caller)
        foreach (var header in request.Headers)
        {
            if (!string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Set content from pre-read bytes if present
        if (contentBytes != null)
        {
            clone.Content = new ByteArrayContent(contentBytes);
            
            if (request.Content != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
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

    private class TokenRefreshResult
    {
        public bool Success { get; set; }
        public string? NewAccessToken { get; set; }
    }
}
