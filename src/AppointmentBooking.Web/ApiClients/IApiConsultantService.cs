using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ApiClients;

/// <summary>
/// API client interface for consultant-related operations
/// </summary>
public interface IApiConsultantService
{
    Task<ConsultantDto?> GetConsultantByIdAsync(int id);
    Task<ConsultantDto?> GetConsultantByUserIdAsync(int userId);
    Task<IEnumerable<ConsultantDto>> GetAllConsultantsAsync();
    Task<IEnumerable<ConsultantDto>> GetConsultantsByBranchAsync(int branchId);
    Task<ConsultantScheduleDto?> GetConsultantScheduleAsync(int consultantId, DateTime date);
    Task<ConsultantRegistrationResultDto> RegisterConsultantAsync(ConsultantRegistrationDto dto);
    Task<IEnumerable<PendingConsultantDto>> GetPendingConsultantsAsync();
    Task<bool> ActivateConsultantAsync(int consultantId);
    Task<bool> RejectConsultantAsync(int consultantId);
}
