using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ApiClients;

/// <summary>
/// API client interface for availability-related operations
/// </summary>
public interface IApiAvailabilityService
{
    Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(int branchId, int serviceId, DateTime date);
    Task<IEnumerable<DateTime>> GetAvailableDatesAsync(int branchId, int daysAhead = 30);
}
