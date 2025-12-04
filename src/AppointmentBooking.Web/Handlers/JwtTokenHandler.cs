using System.Net.Http.Headers;

namespace AppointmentBooking.Web.Handlers;

/// <summary>
/// HTTP message handler that automatically attaches JWT token from session to API requests
/// </summary>
public class JwtTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<JwtTokenHandler> _logger;

    public JwtTokenHandler(IHttpContextAccessor httpContextAccessor, ILogger<JwtTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get JWT token from session
        var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");

        if (!string.IsNullOrEmpty(token))
        {
            // Attach token to Authorization header
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("JWT token attached to request: {Method} {Uri}", request.Method, request.RequestUri);
        }
        else
        {
            _logger.LogDebug("No JWT token in session for request: {Method} {Uri}", request.Method, request.RequestUri);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
