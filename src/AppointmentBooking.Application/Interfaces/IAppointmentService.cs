using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto);
    Task<AppointmentDto?> GetAppointmentByIdAsync(int id);
    Task<AppointmentDto?> GetAppointmentByConfirmationCodeAsync(string confirmationCode);
    Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync();
    Task<IEnumerable<AppointmentDto>> GetAppointmentsByCustomerEmailAsync(string email);
    Task<IEnumerable<AppointmentDto>> GetAppointmentsByBranchAsync(int branchId);
    Task<AppointmentDto> UpdateAppointmentAsync(UpdateAppointmentDto dto);
    Task<bool> CancelAppointmentAsync(int id, string cancellationReason);
    Task<bool> UpdateAppointmentStatusAsync(int id, AppointmentStatus status);
    
    // Consultant appointment views
    Task<IEnumerable<AppointmentDto>> GetConsultantUpcomingAppointmentsAsync(int consultantId);
    Task<IEnumerable<AppointmentDto>> GetConsultantPastAppointmentsAsync(int consultantId);
    Task<IEnumerable<AppointmentDto>> GetConsultantTodayAppointmentsAsync(int consultantId);
    Task<IEnumerable<AppointmentDto>> GetConsultantAppointmentsByDateRangeAsync(int consultantId, DateTime startDate, DateTime endDate);
    
    // Customer appointment views with filtering
    Task<IEnumerable<AppointmentDto>> GetCustomerUpcomingAppointmentsAsync(string email);
    Task<IEnumerable<AppointmentDto>> GetCustomerPastAppointmentsAsync(string email);
    Task<IEnumerable<AppointmentDto>> GetCustomerAppointmentsByStatusAsync(string email, AppointmentStatus status);
    Task<IEnumerable<AppointmentDto>> GetCustomerAppointmentsByDateRangeAsync(string email, DateTime startDate, DateTime endDate);
}
