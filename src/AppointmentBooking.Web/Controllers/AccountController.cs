using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Web.Models;

namespace AppointmentBooking.Web.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AccountController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("AuthApi");
            var loginPayload = new
            {
                email = model.Email,
                password = model.Password
            };

            var content = new StringContent(
                JsonSerializer.Serialize(loginPayload),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("/api/auth/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (authResponse != null)
                {
                    await SignInUserAsync(authResponse, model.RememberMe);
                    _logger.LogInformation("User {Email} logged in successfully", model.Email);
                    return RedirectToLocal(model.ReturnUrl);
                }
            }

            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ModelState.AddModelError(string.Empty, errorResponse?.Message ?? "Invalid login attempt.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("AuthApi");
            var registerPayload = new
            {
                firstName = model.FirstName,
                lastName = model.LastName,
                email = model.Email,
                password = model.Password,
                confirmPassword = model.ConfirmPassword
            };

            var content = new StringContent(
                JsonSerializer.Serialize(registerPayload),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("/api/auth/register", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (authResponse != null)
                {
                    await SignInUserAsync(authResponse, rememberMe: false);
                    _logger.LogInformation("User {Email} registered successfully", model.Email);
                    return RedirectToLocal(model.ReturnUrl);
                }
            }

            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ModelState.AddModelError(string.Empty, errorResponse?.Message ?? "Registration failed. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var refreshToken = Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var client = _httpClientFactory.CreateClient("AuthApi");
                var logoutPayload = new { refreshToken };

                var content = new StringContent(
                    JsonSerializer.Serialize(logoutPayload),
                    Encoding.UTF8,
                    "application/json");

                await client.PostAsync("/api/auth/logout", content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout API call");
        }

        // Clear cookies
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");

        // Sign out from cookie authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User logged out");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInUserAsync(AuthResponse authResponse, bool rememberMe)
    {
        // Set secure HttpOnly cookies for tokens
        var accessTokenOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(authResponse.AccessTokenExpiryMinutes)
        };

        var refreshTokenExpiry = rememberMe
            ? DateTimeOffset.UtcNow.AddDays(authResponse.RefreshTokenExpiryDays)
            : (DateTimeOffset?)null;

        var refreshTokenOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshTokenExpiry
        };

        Response.Cookies.Append("access_token", authResponse.AccessToken, accessTokenOptions);
        Response.Cookies.Append("refresh_token", authResponse.RefreshToken, refreshTokenOptions);

        // Create claims principal for cookie authentication
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authResponse.User.Id.ToString()),
            new(ClaimTypes.Email, authResponse.User.Email),
            new(ClaimTypes.Name, $"{authResponse.User.FirstName} {authResponse.User.LastName}"),
            new(ClaimTypes.GivenName, authResponse.User.FirstName),
            new(ClaimTypes.Surname, authResponse.User.LastName),
            new(ClaimTypes.Role, authResponse.User.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = refreshTokenExpiry ?? DateTimeOffset.UtcNow.AddMinutes(authResponse.AccessTokenExpiryMinutes)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    private class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int AccessTokenExpiryMinutes { get; set; } = 60;
        public int RefreshTokenExpiryDays { get; set; } = 7;
        public UserInfo User { get; set; } = new();
    }

    private class UserInfo
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
    }

    private class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
