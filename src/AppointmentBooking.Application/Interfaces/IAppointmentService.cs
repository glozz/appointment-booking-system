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
}
