using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Web.ApiClients;
using AppointmentBooking.Web.Models;

namespace AppointmentBooking.Web.Controllers;

/// <summary>
/// Controller for consultant self-registration (public access)
/// </summary>
public class ConsultantRegistrationController : Controller
{
    private readonly IApiConsultantService _consultantService;
    private readonly IApiBranchService _branchService;
    private readonly ILogger<ConsultantRegistrationController> _logger;

    public ConsultantRegistrationController(
        IApiConsultantService consultantService,
        IApiBranchService branchService,
        ILogger<ConsultantRegistrationController> logger)
    {
        _consultantService = consultantService;
        _branchService = branchService;
        _logger = logger;
    }

    /// <summary>
    /// Display consultant registration form
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        var model = await PrepareRegistrationViewModel(new ConsultantRegistrationViewModel());
        return View(model);
    }

    /// <summary>
    /// Process consultant registration
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(ConsultantRegistrationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await PrepareRegistrationViewModel(model);
            return View(model);
        }

        var dto = new ConsultantRegistrationDto
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword,
            BranchId = model.BranchId,
            Phone = model.Phone
        };

        var result = await _consultantService.RegisterConsultantAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Registration failed. Please try again.");
            model = await PrepareRegistrationViewModel(model);
            return View(model);
        }

        _logger.LogInformation("Consultant registration successful for {Email}", model.Email);
        return RedirectToAction(nameof(RegistrationSuccess));
    }

    /// <summary>
    /// Display registration success page
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegistrationSuccess()
    {
        return View();
    }

    /// <summary>
    /// Prepare the registration view model with branch dropdown data
    /// </summary>
    private async Task<ConsultantRegistrationViewModel> PrepareRegistrationViewModel(ConsultantRegistrationViewModel model)
    {
        var branches = await _branchService.GetActiveBranchesAsync();
        model.Branches = branches
            .Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = $"{b.Name} - {b.City}"
            })
            .ToList();

        return model;
    }
}
