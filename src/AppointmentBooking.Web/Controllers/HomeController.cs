using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Web.ApiClients;
using AppointmentBooking.Web.Models;

namespace AppointmentBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly IApiBranchService _branchService;
    private readonly IApiServiceService _serviceService;

    public HomeController(IApiBranchService branchService, IApiServiceService serviceService)
    {
        _branchService = branchService;
        _serviceService = serviceService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var branches = await _branchService.GetActiveBranchesAsync();
        var services = await _serviceService.GetActiveServicesAsync();
        
        var viewModel = new HomeViewModel
        {
            Branches = branches,
            Services = services
        };
        
        return View(viewModel);
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
