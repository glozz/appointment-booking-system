using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Exceptions;

namespace AppointmentBooking.Web.Controllers;

public class AppointmentsController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly IBranchService _branchService;
    private readonly IServiceService _serviceService;
    private readonly IAvailabilityService _availabilityService;

    public AppointmentsController(
        IAppointmentService appointmentService,
        IBranchService branchService,
        IServiceService serviceService,
        IAvailabilityService availabilityService)
    {
        _appointmentService = appointmentService;
        _branchService = branchService;
        _serviceService = serviceService;
        _availabilityService = availabilityService;
    }

    public async Task<IActionResult> Book()
    {
        ViewBag.Branches = await _branchService.GetActiveBranchesAsync();
        ViewBag.Services = await _serviceService.GetActiveServicesAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Book(CreateAppointmentDto dto)
    {
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
