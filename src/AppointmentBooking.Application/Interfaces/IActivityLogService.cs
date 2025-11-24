using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Application.Interfaces;

public interface IActivityLogService
{
    Task LogActivityAsync(int? userId, string action, string? entityType = null, int? entityId = null, 
        string? details = null, string? ipAddress = null, string? userAgent = null);
    Task<IEnumerable<ActivityLog>> GetUserActivityAsync(int userId, int limit = 50);
    Task<IEnumerable<ActivityLog>> GetRecentActivityAsync(int limit = 20);
}
