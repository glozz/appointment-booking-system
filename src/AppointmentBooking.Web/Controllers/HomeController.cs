using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Web.Models;

namespace AppointmentBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly IBranchService _branchService;
    private readonly IServiceService _serviceService;

    public HomeController(IBranchService branchService, IServiceService serviceService)
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
