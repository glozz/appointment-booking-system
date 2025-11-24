using Microsoft.Extensions.Logging;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;
using AppointmentBooking.Application.Interfaces;

namespace AppointmentBooking.Application.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(IUnitOfWork unitOfWork, ILogger<ActivityLogService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogActivityAsync(int? userId, string action, string? entityType = null, int? entityId = null, 
        string? details = null, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ActivityLogs.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log activity: {Action}", action);
        }
    }

    public async Task<IEnumerable<ActivityLog>> GetUserActivityAsync(int userId, int limit = 50)
    {
        var activities = await _unitOfWork.ActivityLogs.FindAsync(a => a.UserId == userId);
        return activities.OrderByDescending(a => a.CreatedAt).Take(limit);
    }

    public async Task<IEnumerable<ActivityLog>> GetRecentActivityAsync(int limit = 20)
    {
        var activities = await _unitOfWork.ActivityLogs.GetAllAsync();
        return activities.OrderByDescending(a => a.CreatedAt).Take(limit);
    }
}
