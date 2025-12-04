using System.Net.Http.Json;
using System.Text.Json;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Web.ApiClients;

/// <summary>
/// API client for appointment-related operations
/// </summary>
public class ApiAppointmentService : IApiAppointmentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiAppointmentService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiAppointmentService(HttpClient httpClient, ILogger<ApiAppointmentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AppointmentDto?> CreateAppointmentAsync(CreateAppointmentDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/appointments", dto);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to create appointment. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, errorContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment via API");
            return null;
        }
    }

    public async Task<AppointmentDto?> GetAppointmentByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AppointmentDto>($"api/appointments/{id}", JsonOptions);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching appointment {AppointmentId} from API", id);
            return null;
        }
    }

    public async Task<AppointmentDto?> GetAppointmentByConfirmationCodeAsync(string confirmationCode)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AppointmentDto>(
                $"api/appointments/confirmation/{Uri.EscapeDataString(confirmationCode)}", JsonOptions);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching appointment by confirmation code {Code} from API", confirmationCode);
            return null;
        }
    }

    public async Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AppointmentDto>>("api/appointments", JsonOptions);
            return response ?? Enumerable.Empty<AppointmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all appointments from API");
            return Enumerable.Empty<AppointmentDto>();
        }
    }

    public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByCustomerEmailAsync(string email)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AppointmentDto>>(
                $"api/appointments/customer/{Uri.EscapeDataString(email)}", JsonOptions);
            return response ?? Enumerable.Empty<AppointmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching appointments for customer {Email} from API", email);
            return Enumerable.Empty<AppointmentDto>();
        }
    }

    public async Task<bool> CancelAppointmentAsync(int id, string cancellationReason)
    {
        try
        {
            var request = new { Reason = cancellationReason };
            var response = await _httpClient.PostAsJsonAsync($"api/appointments/{id}/cancel", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling appointment {AppointmentId} via API", id);
            return false;
        }
    }

    public async Task<bool> UpdateAppointmentStatusAsync(int id, AppointmentStatus status)
    {
        try
        {
            var request = new { Status = status };
            var response = await _httpClient.PatchAsJsonAsync($"api/appointments/{id}/status", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment {AppointmentId} status to {Status} via API", id, status);
            return false;
        }
    }

    // Customer appointment views
    public async Task<IEnumerable<AppointmentDto>> GetCustomerUpcomingAppointmentsAsync(string email)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AppointmentDto>>(
                "api/appointments/my/upcoming", JsonOptions);
            return response ?? Enumerable.Empty<AppointmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching upcoming appointments for customer from API");
            return Enumerable.Empty<AppointmentDto>();
        }
    }

    public async Task<IEnumerable<AppointmentDto>> GetCustomerPastAppointmentsAsync(string email)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AppointmentDto>>(
                "api/appointments/my/past", JsonOptions);
            return response ?? Enumerable.Empty<AppointmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching past appointments for customer from API");
            return Enumerable.Empty<AppointmentDto>();
        }
    }

    public async Task<IEnumerable<AppointmentDto>> GetCustomerAppointmentsByStatusAsync(string email, AppointmentStatus status)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AppointmentDto>>(
                $"api/appointments/my/status/{status}", JsonOptions);
            return response ?? Enumerable.Empty<AppointmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching appointments by status {Status} for customer from API", status);
            return Enumerable.Empty<AppointmentDto>();
        }
    }

    public async Task<IEnumerable<AppointmentDto>> GetCustomerAppointmentsByDateRangeAsync(string email, DateTime startDate, DateTime endDate)
    {
        try
        {
            var startDateString = startDate.ToString("yyyy-MM-dd");
            var endDateString = endDate.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AppointmentDto>>(
                $"api/appointments/my?startDate={startDateString}&endDate={endDateString}", JsonOptions);
            return response ?? Enumerable.Empty<AppointmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching appointments by date range for customer from API");
            return Enumerable.Empty<AppointmentDto>();
        }
    }

    // Consultant appointment views
    public async Task<IEnumerable<AppointmentDto>> GetConsultantUpcomingAppointmentsAsync(int consultantId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AppointmentDto>>(
                $"api/consultants/{consultantId}/appointments/upcoming", JsonOptions);
            return response ?? Enumerable.Empty<AppointmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching upcoming appointments for consultant {ConsultantId} from API", consultantId);
            return Enumerable.Empty<AppointmentDto>();
        }
    }

    public async Task<IEnumerable<AppointmentDto>> GetConsultantPastAppointmentsAsync(int consultantId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AppointmentDto>>(
                $"api/consultants/{consultantId}/appointments/past", JsonOptions);
            return response ?? Enumerable.Empty<AppointmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching past appointments for consultant {ConsultantId} from API", consultantId);
            return Enumerable.Empty<AppointmentDto>();
        }
    }

    public async Task<IEnumerable<AppointmentDto>> GetConsultantTodayAppointmentsAsync(int consultantId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AppointmentDto>>(
                $"api/consultants/{consultantId}/appointments/today", JsonOptions);
            return response ?? Enumerable.Empty<AppointmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching today's appointments for consultant {ConsultantId} from API", consultantId);
            return Enumerable.Empty<AppointmentDto>();
        }
    }

    public async Task<IEnumerable<AppointmentDto>> GetConsultantAppointmentsByDateRangeAsync(int consultantId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var startDateString = startDate.ToString("yyyy-MM-dd");
            var endDateString = endDate.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AppointmentDto>>(
                $"api/consultants/{consultantId}/appointments?startDate={startDateString}&endDate={endDateString}", JsonOptions);
            return response ?? Enumerable.Empty<AppointmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching appointments by date range for consultant {ConsultantId} from API", consultantId);
            return Enumerable.Empty<AppointmentDto>();
        }
    }
}
