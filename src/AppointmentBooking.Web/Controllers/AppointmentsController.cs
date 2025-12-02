using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Exceptions;
using AppointmentBooking.Core.Interfaces;

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
    private readonly IAppointmentService _appointmentService;
    private readonly IBranchService _branchService;
    private readonly IServiceService _serviceService;
    private readonly IAvailabilityService _availabilityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentService appointmentService,
        IBranchService branchService,
        IServiceService serviceService,
        IAvailabilityService availabilityService,
        IUnitOfWork unitOfWork,
        ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _branchService = branchService;
        _serviceService = serviceService;
        _availabilityService = availabilityService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Book()
    {
        ViewBag.Branches = await _branchService.GetActiveBranchesAsync();
        ViewBag.Services = await _serviceService.GetActiveServicesAsync();
        
        // Pre-populate customer details from logged-in user
        var model = new CreateAppointmentDto
        {
            Customer = await GetLoggedInUserCustomerDtoAsync()
        };
        
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Book(CreateAppointmentDto dto)
    {
        // Re-resolve customer from authenticated user (don't trust posted values)
        dto.Customer = await GetLoggedInUserCustomerDtoAsync();
        
        if (!ModelState.IsValid)
        {
            ViewBag.Branches = await _branchService.GetActiveBranchesAsync();
            ViewBag.Services = await _serviceService.GetActiveServicesAsync();
            return View(dto);
        }

        try
        {
            var appointment = await _appointmentService.CreateAppointmentAsync(dto);
            return RedirectToAction(nameof(Confirmation), new { code = appointment.ConfirmationCode });
        }
        catch (ConflictException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Branches = await _branchService.GetActiveBranchesAsync();
            ViewBag.Services = await _serviceService.GetActiveServicesAsync();
            return View(dto);
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Branches = await _branchService.GetActiveBranchesAsync();
            ViewBag.Services = await _serviceService.GetActiveServicesAsync();
            return View(dto);
        }
    }

    /// <summary>
    /// Retrieves customer information from the authenticated user's claims and existing records.
    /// 
    /// Security: This method ONLY uses the email from authenticated claims, not from user input.
    /// This ensures users can only access their own customer data.
    /// 
    /// Flow:
    /// 1. Get authenticated user's email from JWT/cookie claims
    /// 2. Check if a Customer record exists with that email
    /// 3. If Customer exists, use that data (may have more complete info like phone)
    /// 4. If not, fall back to User record for additional details
    /// 5. Return CustomerDto with available data
    /// </summary>
    private async Task<CustomerDto> GetLoggedInUserCustomerDtoAsync()
    {
        // SECURITY: Email is retrieved from authenticated claims, not user input
        var authenticatedEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var firstName = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var lastName = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        
        if (string.IsNullOrEmpty(authenticatedEmail))
        {
            _logger.LogWarning("Authenticated user has no email claim. User identity: {Identity}", 
                User.Identity?.Name ?? "unknown");
            return new CustomerDto
            {
                FirstName = firstName,
                LastName = lastName,
                Email = string.Empty,
                Phone = string.Empty
            };
        }
        
        _logger.LogDebug("Retrieving customer data for authenticated user: {Email}", authenticatedEmail);
        
        var phone = string.Empty;
        
        // First, try to get data from existing Customer record
        var customers = await _unitOfWork.Customers.FindAsync(c => c.Email == authenticatedEmail);
        var existingCustomer = customers.FirstOrDefault();
        
        if (existingCustomer != null)
        {
            // Use existing customer data - it may have more complete information
            firstName = !string.IsNullOrEmpty(firstName) ? firstName : existingCustomer.FirstName;
            lastName = !string.IsNullOrEmpty(lastName) ? lastName : existingCustomer.LastName;
            phone = existingCustomer.Phone;
        }
        else
        {
            // Fall back to User record for additional details (like phone)
            var users = await _unitOfWork.Users.FindAsync(u => u.Email == authenticatedEmail);
            var user = users.FirstOrDefault();
            if (user != null)
            {
                phone = user.Phone ?? string.Empty;
                firstName = !string.IsNullOrEmpty(firstName) ? firstName : user.FirstName;
                lastName = !string.IsNullOrEmpty(lastName) ? lastName : user.LastName;
            }
        }
        
        return new CustomerDto
        {
            FirstName = firstName,
            LastName = lastName,
            Email = authenticatedEmail,
            Phone = phone
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
        try
        {
            await _appointmentService.CancelAppointmentAsync(id, reason);
            return RedirectToAction(nameof(CancelConfirmation));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
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
