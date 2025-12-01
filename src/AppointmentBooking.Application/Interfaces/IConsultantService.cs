using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Application.Interfaces;

public interface IConsultantService
{
    /// <summary>
    /// Get consultant by ID
    /// </summary>
    Task<ConsultantDto?> GetConsultantByIdAsync(int id);
    
    /// <summary>
    /// Get consultant by associated user ID
    /// </summary>
    Task<ConsultantDto?> GetConsultantByUserIdAsync(int userId);
    
    /// <summary>
    /// Get all consultants
    /// </summary>
    Task<IEnumerable<ConsultantDto>> GetAllConsultantsAsync();
    
    /// <summary>
    /// Get consultants by branch
    /// </summary>
    Task<IEnumerable<ConsultantDto>> GetConsultantsByBranchAsync(int branchId);
    
    /// <summary>
    /// Get a consultant's schedule for a specific date
    /// </summary>
    Task<ConsultantScheduleDto?> GetConsultantScheduleAsync(int consultantId, DateTime date);
}
