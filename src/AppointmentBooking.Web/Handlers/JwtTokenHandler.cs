using System.Net.Http.Headers;

namespace AppointmentBooking.Web.Handlers;

/// <summary>
/// DelegatingHandler that attaches JWT token from session to outgoing HttpClient requests
/// </summary>
public class JwtTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<JwtTokenHandler> _logger;

    public JwtTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<JwtTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // Attach JWT token from session if available
        var jwtToken = httpContext?.Session.GetString("JwtToken");
        if (!string.IsNullOrEmpty(jwtToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            _logger.LogDebug("Attached JWT token to request for {Uri}", request.RequestUri);
        }
        else
        {
            _logger.LogDebug("No JWT token found in session for request to {Uri}", request.RequestUri);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
