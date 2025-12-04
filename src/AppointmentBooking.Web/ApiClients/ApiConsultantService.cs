using System.Net.Http.Json;
using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ApiClients;

/// <summary>
/// API client for consultant-related operations
/// </summary>
public class ApiConsultantService : IApiConsultantService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiConsultantService> _logger;

    public ApiConsultantService(HttpClient httpClient, ILogger<ApiConsultantService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ConsultantDto?> GetConsultantByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ConsultantDto>($"api/consultants/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching consultant {ConsultantId} from API", id);
            return null;
        }
    }

    public async Task<ConsultantDto?> GetConsultantByUserIdAsync(int userId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ConsultantDto>($"api/consultants/user/{userId}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching consultant by user {UserId} from API", userId);
            return null;
        }
    }

    public async Task<IEnumerable<ConsultantDto>> GetAllConsultantsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ConsultantDto>>("api/consultants");
            return response ?? Enumerable.Empty<ConsultantDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all consultants from API");
            return Enumerable.Empty<ConsultantDto>();
        }
    }

    public async Task<IEnumerable<ConsultantDto>> GetConsultantsByBranchAsync(int branchId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ConsultantDto>>(
                $"api/consultants/branch/{branchId}");
            return response ?? Enumerable.Empty<ConsultantDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching consultants for branch {BranchId} from API", branchId);
            return Enumerable.Empty<ConsultantDto>();
        }
    }

    public async Task<ConsultantScheduleDto?> GetConsultantScheduleAsync(int consultantId, DateTime date)
    {
        try
        {
            var dateString = date.ToString("yyyy-MM-dd");
            return await _httpClient.GetFromJsonAsync<ConsultantScheduleDto>(
                $"api/consultants/{consultantId}/schedule?date={dateString}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching schedule for consultant {ConsultantId} on {Date} from API",
                consultantId, date);
            return null;
        }
    }

    public async Task<ConsultantRegistrationResultDto> RegisterConsultantAsync(ConsultantRegistrationDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/consultants/register", dto);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ConsultantRegistrationResultDto>();
                return result ?? new ConsultantRegistrationResultDto
                {
                    Success = false,
                    Message = "Failed to parse response"
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return new ConsultantRegistrationResultDto
            {
                Success = false,
                Message = $"Registration failed: {errorContent}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering consultant via API");
            return new ConsultantRegistrationResultDto
            {
                Success = false,
                Message = "An error occurred during registration"
            };
        }
    }

    public async Task<IEnumerable<PendingConsultantDto>> GetPendingConsultantsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<PendingConsultantDto>>(
                "api/consultants/pending");
            return response ?? Enumerable.Empty<PendingConsultantDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending consultants from API");
            return Enumerable.Empty<PendingConsultantDto>();
        }
    }

    public async Task<bool> ActivateConsultantAsync(int consultantId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/consultants/{consultantId}/activate", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating consultant {ConsultantId} via API", consultantId);
            return false;
        }
    }

    public async Task<bool> RejectConsultantAsync(int consultantId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/consultants/{consultantId}/reject", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting consultant {ConsultantId} via API", consultantId);
            return false;
        }
    }
}
