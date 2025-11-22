using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Application.Interfaces;

public interface IServiceService
{
    Task<ServiceDto> CreateServiceAsync(ServiceDto dto);
    Task<ServiceDto?> GetServiceByIdAsync(int id);
    Task<IEnumerable<ServiceDto>> GetAllServicesAsync();
    Task<IEnumerable<ServiceDto>> GetActiveServicesAsync();
    Task<ServiceDto> UpdateServiceAsync(ServiceDto dto);
    Task<bool> DeleteServiceAsync(int id);
}
