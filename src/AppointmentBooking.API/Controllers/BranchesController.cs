using Microsoft.AspNetCore.Mvc;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;

namespace AppointmentBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;
    private readonly IAvailabilityService _availabilityService;

    public BranchesController(IBranchService branchService, IAvailabilityService availabilityService)
    {
        _branchService = branchService;
        _availabilityService = availabilityService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BranchDto>>> GetAllBranches()
    {
        var branches = await _branchService.GetAllBranchesAsync();
        return Ok(branches);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<BranchDto>>> GetActiveBranches()
    {
        var branches = await _branchService.GetActiveBranchesAsync();
        return Ok(branches);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BranchDto>> GetBranch(int id)
    {
        var branch = await _branchService.GetBranchByIdAsync(id);
        return branch == null ? NotFound() : Ok(branch);
    }

    [HttpGet("{id}/availability")]
    public async Task<ActionResult<IEnumerable<AvailableSlotDto>>> GetAvailableSlots(
        int id, [FromQuery] int serviceId, [FromQuery] DateTime date)
    {
        if (serviceId <= 0)
            return BadRequest(new { message = "Invalid serviceId" });
            
        if (date == default)
            return BadRequest(new { message = "Invalid date" });
            
        var slots = await _availabilityService.GetAvailableSlotsAsync(id, serviceId, date);
        return Ok(slots);
    }

    [HttpGet("{id}/available-dates")]
    public async Task<ActionResult<IEnumerable<DateTime>>> GetAvailableDates(int id, [FromQuery] int daysAhead = 30)
    {
        var dates = await _availabilityService.GetAvailableDatesAsync(id, daysAhead);
        return Ok(dates);
    }

    [HttpPost]
    public async Task<ActionResult<BranchDto>> CreateBranch([FromBody] CreateBranchDto dto)
    {
        var branch = await _branchService.CreateBranchAsync(dto);
        return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, branch);
    }
}
