using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Web.Models;
using AppointmentBooking.Web.Services.ApiClients;
using AppointmentBooking.Application.DTOs.Auth;
using Microsoft.AspNetCore.Identity.Data;

namespace AppointmentBooking.Web.Controllers;


public class AccountController : Controller
{
    private readonly IAuthApiClient _authClient;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthApiClient authClient, ILogger<AccountController> logger)
    {
        _authClient = authClient;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        HttpContext.Session.Clear();
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var request = new LoginRequest
            {
                Email = model.Email,
                Password = model.Password,
                RememberMe = model.RememberMe
            };

            var response = await _authClient.LoginAsync(request);

            if (response == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                return View(model);
            }

            // Store JWT token in session
            HttpContext.Session.SetString("JwtToken", response.Token);
            HttpContext.Session.SetString("UserEmail", response.Email);
            HttpContext.Session.SetString("UserFullName", response.FullName);
            HttpContext.Session.SetInt32("UserId", response.UserId);
            HttpContext.Session.SetString("UserRoles", string.Join(",", response.Roles));

            _logger.LogInformation("User {Email} logged in via API", model.Email);

            // Redirect based on return URL or role
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (response.Roles.Contains("Admin"))
                return RedirectToAction("Index", "AdminDashboard");
            else if (response.Roles.Contains("Consultant"))
                return RedirectToAction("Dashboard", "ConsultantPortal");
            else
                return RedirectToAction("Index", "Home");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Cannot connect to API");
            ModelState.AddModelError(string.Empty, "Unable to connect to authentication service.  Please ensure the API is running.");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            ModelState.AddModelError(string.Empty, "An error occurred.  Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var request = new RegisterRequest
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                Phone = model.Phone
            };

            var response = await _authClient.RegisterAsync(request);

            if (response == null)
            {
                ModelState.AddModelError(string.Empty, "Registration failed. Email may already be in use.");
                return View(model);
            }

            // Store JWT token in session
            HttpContext.Session.SetString("JwtToken", response.Token);
            HttpContext.Session.SetString("UserEmail", response.Email);
            HttpContext.Session.SetString("UserFullName", response.FullName);
            HttpContext.Session.SetInt32("UserId", response.UserId);
            HttpContext.Session.SetString("UserRoles", string.Join(",", response.Roles));

            _logger.LogInformation("User {Email} registered via API", model.Email);

            return RedirectToAction("Index", "Home");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Cannot connect to API");
            ModelState.AddModelError(string.Empty, "Unable to connect to authentication service.");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error");
            ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        _logger.LogInformation("User logged out");
        return RedirectToAction("Index", "Home");
    }
    
    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
}