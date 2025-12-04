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
    
    /// <summary>
    /// Register a new consultant (creates inactive user and consultant profile)
    /// </summary>
    Task<ConsultantRegistrationResultDto> RegisterConsultantAsync(ConsultantRegistrationDto dto);
    
    /// <summary>
    /// Get all pending (inactive) consultants for admin review
    /// </summary>
    Task<IEnumerable<PendingConsultantDto>> GetPendingConsultantsAsync();
    
    /// <summary>
    /// Activate a pending consultant (activates both user and consultant profile)
    /// </summary>
    Task<bool> ActivateConsultantAsync(int consultantId);
    
    /// <summary>
    /// Reject and delete a pending consultant registration
    /// </summary>
    Task<bool> RejectConsultantAsync(int consultantId);
}
