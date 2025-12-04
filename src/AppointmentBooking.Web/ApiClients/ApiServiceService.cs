using System.Net.Http.Json;
using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ApiClients;

/// <summary>
/// API client for service-related operations
/// </summary>
public class ApiServiceService : IApiServiceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiServiceService> _logger;

    public ApiServiceService(HttpClient httpClient, ILogger<ApiServiceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<ServiceDto>> GetAllServicesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ServiceDto>>("api/services");
            return response ?? Enumerable.Empty<ServiceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all services from API");
            return Enumerable.Empty<ServiceDto>();
        }
    }

    public async Task<IEnumerable<ServiceDto>> GetActiveServicesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ServiceDto>>("api/services/active");
            return response ?? Enumerable.Empty<ServiceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active services from API");
            return Enumerable.Empty<ServiceDto>();
        }
    }

    public async Task<ServiceDto?> GetServiceByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ServiceDto>($"api/services/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching service {ServiceId} from API", id);
            return null;
        }
    }
}
