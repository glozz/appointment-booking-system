using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ApiClients;

/// <summary>
/// API client interface for service-related operations
/// </summary>
public interface IApiServiceService
{
    Task<IEnumerable<ServiceDto>> GetAllServicesAsync();
    Task<IEnumerable<ServiceDto>> GetActiveServicesAsync();
    Task<ServiceDto?> GetServiceByIdAsync(int id);
}
