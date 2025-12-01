using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Exceptions;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Web.Controllers;

[Authorize]
public class AppointmentsController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly IBranchService _branchService;
    private readonly IServiceService _serviceService;
    private readonly IAvailabilityService _availabilityService;
    private readonly IUnitOfWork _unitOfWork;

    public AppointmentsController(
        IAppointmentService appointmentService,
        IBranchService branchService,
        IServiceService serviceService,
        IAvailabilityService availabilityService,
        IUnitOfWork unitOfWork)
    {
        _appointmentService = appointmentService;
        _branchService = branchService;
        _serviceService = serviceService;
        _availabilityService = availabilityService;
        _unitOfWork = unitOfWork;
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

            if (dto.Customer.Phone.StartsWith("0"))
            {
                dto.Customer.Phone = "+27" + dto.Customer.Phone.Substring(1);
            }

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
    /// Gets the customer DTO from the logged-in user's claims
    /// </summary>
    private async Task<CustomerDto> GetLoggedInUserCustomerDtoAsync()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var firstName = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var lastName = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        
        // Try to get additional info (like phone) from existing customer record
        var phone = string.Empty;
        if (!string.IsNullOrEmpty(email))
        {
            var customers = await _unitOfWork.Customers.FindAsync(c => c.Email == email);
            var existingCustomer = customers.FirstOrDefault();
            if (existingCustomer != null)
            {
                // Use existing customer data if found
                firstName = !string.IsNullOrEmpty(firstName) ? firstName : existingCustomer.FirstName;
                lastName = !string.IsNullOrEmpty(lastName) ? lastName : existingCustomer.LastName;
                phone = existingCustomer.Phone;
            }
            else
            {
                // If no existing customer, try to get phone from User record
                var users = await _unitOfWork.Users.FindAsync(u => u.Email == email);
                var user = users.FirstOrDefault();
                if (user != null)
                {
                    phone = user.Phone ?? string.Empty;
                    firstName = !string.IsNullOrEmpty(firstName) ? firstName : user.FirstName;
                    lastName = !string.IsNullOrEmpty(lastName) ? lastName : user.LastName;
                }
            }
        }
        
        return new CustomerDto
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
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
}
