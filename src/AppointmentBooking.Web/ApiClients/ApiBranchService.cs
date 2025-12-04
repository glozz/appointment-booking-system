using System.Net.Http.Json;
using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.ApiClients;

/// <summary>
/// API client for branch-related operations
/// </summary>
public class ApiBranchService : IApiBranchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiBranchService> _logger;

    public ApiBranchService(HttpClient httpClient, ILogger<ApiBranchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<BranchDto>> GetAllBranchesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<BranchDto>>("api/branches");
            return response ?? Enumerable.Empty<BranchDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all branches from API");
            return Enumerable.Empty<BranchDto>();
        }
    }

    public async Task<IEnumerable<BranchDto>> GetActiveBranchesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<BranchDto>>("api/branches/active");
            return response ?? Enumerable.Empty<BranchDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active branches from API");
            return Enumerable.Empty<BranchDto>();
        }
    }

    public async Task<BranchDto?> GetBranchByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BranchDto>($"api/branches/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching branch {BranchId} from API", id);
            return null;
        }
    }
}
