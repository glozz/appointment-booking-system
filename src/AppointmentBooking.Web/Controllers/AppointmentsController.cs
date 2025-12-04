using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Web.ApiClients;

namespace AppointmentBooking.Web.Controllers;

/// <summary>
/// Controller for managing appointments. Requires authentication.
/// 
/// Customer vs User Relationship:
/// - User: An authenticated account in the system (registered user with login credentials)
/// - Customer: A person who books appointments (may or may not have a User account)
/// 
/// When a logged-in User books an appointment:
/// 1. The User's email is used to find or create a Customer record
/// 2. If a Customer with that email exists, their data is used
/// 3. If not, a new Customer is created with User details from claims
/// 
/// Security Note: The authenticated user's email is retrieved from claims (JWT/cookies),
/// not from user input, preventing users from accessing other customers' data.
/// </summary>
[Authorize]
public class AppointmentsController : Controller
{
    private readonly IApiAppointmentService _appointmentService;
    private readonly IApiBranchService _branchService;
    private readonly IApiServiceService _serviceService;
    private readonly IApiAvailabilityService _availabilityService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IApiAppointmentService appointmentService,
        IApiBranchService branchService,
        IApiServiceService serviceService,
        IApiAvailabilityService availabilityService,
        ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _branchService = branchService;
        _serviceService = serviceService;
        _availabilityService = availabilityService;
        _logger = logger;
    }

    public async Task<IActionResult> Book()
    {
        ViewBag.Branches = await _branchService.GetActiveBranchesAsync();
        ViewBag.Services = await _serviceService.GetActiveServicesAsync();
        
        // Pre-populate customer details from logged-in user claims
        var model = new CreateAppointmentDto
        {
            Customer = GetLoggedInUserCustomerDto()
        };
        
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Book(CreateAppointmentDto dto)
    {
        // Re-resolve customer from authenticated user (don't trust posted values)
        dto.Customer = GetLoggedInUserCustomerDto();
        
        if (!ModelState.IsValid)
        {
            ViewBag.Branches = await _branchService.GetActiveBranchesAsync();
            ViewBag.Services = await _serviceService.GetActiveServicesAsync();
            return View(dto);
        }

        var appointment = await _appointmentService.CreateAppointmentAsync(dto);
        if (appointment == null)
        {
            ModelState.AddModelError("", "Failed to create appointment. Please try again.");
            ViewBag.Branches = await _branchService.GetActiveBranchesAsync();
            ViewBag.Services = await _serviceService.GetActiveServicesAsync();
            return View(dto);
        }
        
        return RedirectToAction(nameof(Confirmation), new { code = appointment.ConfirmationCode });
    }

    /// <summary>
    /// Retrieves customer information from the authenticated user's claims.
    /// 
    /// Security: This method ONLY uses the email from authenticated claims, not from user input.
    /// This ensures users can only access their own customer data.
    /// 
    /// The API will handle looking up or creating the Customer record based on this data.
    /// </summary>
    private CustomerDto GetLoggedInUserCustomerDto()
    {
        // SECURITY: Email is retrieved from authenticated claims, not user input
        var authenticatedEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var firstName = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var lastName = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        
        if (string.IsNullOrEmpty(authenticatedEmail))
        {
            _logger.LogWarning("Authenticated user has no email claim. User identity: {Identity}", 
                User.Identity?.Name ?? "unknown");
        }
        
        return new CustomerDto
        {
            FirstName = firstName,
            LastName = lastName,
            Email = authenticatedEmail,
            Phone = string.Empty
        };
    }

    public async Task<IActionResult> Confirmation(string code)
    {
        var appointment = await _appointmentService.GetAppointmentByConfirmationCodeAsync(code);
        if (appointment == null)
            return NotFound();

        return View(appointment);
    }

    public IActionResult Lookup()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Lookup(string confirmationCode)
    {
        var appointment = await _appointmentService.GetAppointmentByConfirmationCodeAsync(confirmationCode);
        if (appointment == null)
        {
            ModelState.AddModelError("", "Appointment not found");
            return View();
        }

        return RedirectToAction(nameof(Details), new { id = appointment.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appointment == null)
            return NotFound();

        return View(appointment);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id, string reason)
    {
        var success = await _appointmentService.CancelAppointmentAsync(id, reason);
        if (!success)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(CancelConfirmation));
    }

    public IActionResult CancelConfirmation()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableSlots(int branchId, int serviceId, DateTime date)
    {
        var slots = await _availabilityService.GetAvailableSlotsAsync(branchId, serviceId, date);
        return Json(slots);
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableDates(int branchId, int daysAhead = 30)
    {
        var dates = await _availabilityService.GetAvailableDatesAsync(branchId, daysAhead);
        return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
    }
}
