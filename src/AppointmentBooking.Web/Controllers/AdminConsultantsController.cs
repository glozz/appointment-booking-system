using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Web.ApiClients;
using AppointmentBooking.Web.Models;

namespace AppointmentBooking.Web.Controllers;

/// <summary>
/// Controller for admin management of consultants
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminConsultantsController : Controller
{
    private readonly IApiConsultantService _consultantService;
    private readonly ILogger<AdminConsultantsController> _logger;

    public AdminConsultantsController(
        IApiConsultantService consultantService,
        ILogger<AdminConsultantsController> logger)
    {
        _consultantService = consultantService;
        _logger = logger;
    }

    /// <summary>
    /// Display pending consultant registrations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Pending()
    {
        var pendingConsultants = await _consultantService.GetPendingConsultantsAsync();
        
        var model = new PendingConsultantsViewModel
        {
            PendingConsultants = pendingConsultants.Select(p => new PendingConsultantViewModel
            {
                ConsultantId = p.ConsultantId,
                UserId = p.UserId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.Phone,
                BranchId = p.BranchId,
                BranchName = p.BranchName,
                RegisteredAt = p.RegisteredAt
            }).ToList()
        };

        return View(model);
    }

    /// <summary>
    /// Activate a pending consultant
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _consultantService.ActivateConsultantAsync(id);

        if (result)
        {
            TempData["SuccessMessage"] = "Consultant has been activated successfully.";
            _logger.LogInformation("Consultant {ConsultantId} activated by admin", id);
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to activate consultant. Please try again.";
            _logger.LogWarning("Failed to activate consultant {ConsultantId}", id);
        }

        return RedirectToAction(nameof(Pending));
    }

    /// <summary>
    /// Reject a pending consultant registration
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var result = await _consultantService.RejectConsultantAsync(id);

        if (result)
        {
            TempData["SuccessMessage"] = "Consultant registration has been rejected and removed.";
            _logger.LogInformation("Consultant {ConsultantId} rejected by admin", id);
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to reject consultant. Please try again.";
            _logger.LogWarning("Failed to reject consultant {ConsultantId}", id);
        }

        return RedirectToAction(nameof(Pending));
    }
}
