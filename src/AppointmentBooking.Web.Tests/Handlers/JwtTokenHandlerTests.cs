using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using AppointmentBooking.Web.Handlers;

namespace AppointmentBooking.Web.Tests.Handlers;

/// <summary>
/// Unit tests for JwtTokenHandler SendAsync behavior.
/// These tests verify the delegating handler's JWT token attachment logic from session.
/// </summary>
public class JwtTokenHandlerTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<JwtTokenHandler>> _loggerMock;

    public JwtTokenHandlerTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<JwtTokenHandler>>();
    }

    [Fact]
    public async Task SendAsync_WithJwtToken_AttachesAuthorizationHeader()
    {
        // Arrange
        var jwtToken = "test-jwt-token";
        var httpContext = CreateMockHttpContextWithSession(jwtToken);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var innerHandler = new TestInnerHandler();
        var handler = CreateHandler(innerHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/api/test");

        // Act
        var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(innerHandler.CapturedRequest);
        Assert.NotNull(innerHandler.CapturedRequest.Headers.Authorization);
        Assert.Equal("Bearer", innerHandler.CapturedRequest.Headers.Authorization.Scheme);
        Assert.Equal(jwtToken, innerHandler.CapturedRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithoutJwtToken_DoesNotAttachAuthorizationHeader()
    {
        // Arrange
        var httpContext = CreateMockHttpContextWithSession(null);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var innerHandler = new TestInnerHandler();
        var handler = CreateHandler(innerHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/api/test");

        // Act
        var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(innerHandler.CapturedRequest);
        Assert.Null(innerHandler.CapturedRequest.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_WithEmptyJwtToken_DoesNotAttachAuthorizationHeader()
    {
        // Arrange
        var httpContext = CreateMockHttpContextWithSession(string.Empty);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var innerHandler = new TestInnerHandler();
        var handler = CreateHandler(innerHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/api/test");

        // Act
        var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(innerHandler.CapturedRequest);
        Assert.Null(innerHandler.CapturedRequest.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_WithNullHttpContext_DoesNotThrow()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var innerHandler = new TestInnerHandler();
        var handler = CreateHandler(innerHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/api/test");

        // Act
        var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(innerHandler.CapturedRequest);
        Assert.Null(innerHandler.CapturedRequest.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_ReturnsResponseFromInnerHandler()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var httpContext = CreateMockHttpContextWithSession("test-token");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var innerHandler = new TestInnerHandler(expectedResponse);
        var handler = CreateHandler(innerHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/api/test");

        // Act
        var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, response);
    }

    private TestableJwtTokenHandler CreateHandler(HttpMessageHandler innerHandler)
    {
        var handler = new TestableJwtTokenHandler(
            _httpContextAccessorMock.Object,
            _loggerMock.Object)
        {
            InnerHandler = innerHandler
        };
        return handler;
    }

    private static HttpContext CreateMockHttpContextWithSession(string? jwtToken)
    {
        var sessionMock = new Mock<ISession>();

        // Session.GetString() is an extension method that calls TryGetValue internally
        // We need to set up TryGetValue to return the proper byte array
        if (!string.IsNullOrEmpty(jwtToken))
        {
            var tokenBytes = System.Text.Encoding.UTF8.GetBytes(jwtToken);
            sessionMock
                .Setup(s => s.TryGetValue("JwtToken", out It.Ref<byte[]?>.IsAny))
                .Callback(new TryGetValueCallback((string key, out byte[]? value) =>
                {
                    value = tokenBytes;
                }))
                .Returns(true);
        }
        else
        {
            sessionMock
                .Setup(s => s.TryGetValue("JwtToken", out It.Ref<byte[]?>.IsAny))
                .Callback(new TryGetValueCallback((string key, out byte[]? value) =>
                {
                    value = null;
                }))
                .Returns(false);
        }

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.Session).Returns(sessionMock.Object);

        return httpContextMock.Object;
    }

    // Delegate for TryGetValue callback
    private delegate void TryGetValueCallback(string key, out byte[]? value);

    /// <summary>
    /// Testable version of JwtTokenHandler that exposes SendAsync for testing
    /// </summary>
    private class TestableJwtTokenHandler : JwtTokenHandler
    {
        public TestableJwtTokenHandler(IHttpContextAccessor httpContextAccessor, ILogger<JwtTokenHandler> logger)
            : base(httpContextAccessor, logger)
        {
        }

        public new Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Inner handler that captures the request for verification
    /// </summary>
    private class TestInnerHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public HttpRequestMessage? CapturedRequest { get; private set; }

        public TestInnerHandler(HttpResponseMessage? response = null)
        {
            _response = response ?? new HttpResponseMessage(HttpStatusCode.OK);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(_response);
        }
    }
}
