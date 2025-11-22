using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Application.Interfaces;

public interface IBranchService
{
    Task<BranchDto> CreateBranchAsync(CreateBranchDto dto);
    Task<BranchDto?> GetBranchByIdAsync(int id);
    Task<IEnumerable<BranchDto>> GetAllBranchesAsync();
    Task<IEnumerable<BranchDto>> GetActiveBranchesAsync();
    Task<BranchDto> UpdateBranchAsync(BranchDto dto);
    Task<bool> DeleteBranchAsync(int id);
}
