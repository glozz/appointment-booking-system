using System.Net.Http.Json;
using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ApiClients;

/// <summary>
/// API client for availability-related operations
/// </summary>
public class ApiAvailabilityService : IApiAvailabilityService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiAvailabilityService> _logger;

    public ApiAvailabilityService(HttpClient httpClient, ILogger<ApiAvailabilityService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(int branchId, int serviceId, DateTime date)
    {
        try
        {
            var dateString = date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AvailableSlotDto>>(
                $"api/branches/{branchId}/availability?serviceId={serviceId}&date={dateString}");
            return response ?? Enumerable.Empty<AvailableSlotDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available slots for branch {BranchId}, service {ServiceId}, date {Date}",
                branchId, serviceId, date);
            return Enumerable.Empty<AvailableSlotDto>();
        }
    }

    public async Task<IEnumerable<DateTime>> GetAvailableDatesAsync(int branchId, int daysAhead = 30)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<DateTime>>(
                $"api/branches/{branchId}/available-dates?daysAhead={daysAhead}");
            return response ?? Enumerable.Empty<DateTime>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available dates for branch {BranchId}", branchId);
            return Enumerable.Empty<DateTime>();
        }
    }
}
