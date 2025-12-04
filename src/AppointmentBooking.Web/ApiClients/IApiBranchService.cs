using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ApiClients;

/// <summary>
/// API client interface for branch-related operations
/// </summary>
public interface IApiBranchService
{
    Task<IEnumerable<BranchDto>> GetAllBranchesAsync();
    Task<IEnumerable<BranchDto>> GetActiveBranchesAsync();
    Task<BranchDto?> GetBranchByIdAsync(int id);
}
