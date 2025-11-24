using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Application.Services;

public class UserNotificationService : IUserNotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserNotificationService> _logger;

    public UserNotificationService(IUnitOfWork unitOfWork, ILogger<UserNotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        var notifications = await _unitOfWork.Notifications.FindAsync(n => 
            n.UserId == userId && (!unreadOnly || !n.IsRead));

        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            });
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        var notifications = await _unitOfWork.Notifications.FindAsync(n => 
            n.UserId == userId && !n.IsRead);
        return notifications.Count();
    }

    public async Task<bool> MarkAsReadAsync(int userId, int notificationId)
    {
        var notifications = await _unitOfWork.Notifications.FindAsync(n => 
            n.Id == notificationId && n.UserId == userId);
        var notification = notifications.FirstOrDefault();

        if (notification == null) return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _unitOfWork.Notifications.UpdateAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var notifications = await _unitOfWork.Notifications.FindAsync(n => 
            n.UserId == userId && !n.IsRead);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _unitOfWork.Notifications.UpdateAsync(notification);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int userId, int notificationId)
    {
        var notifications = await _unitOfWork.Notifications.FindAsync(n => 
            n.Id == notificationId && n.UserId == userId);
        var notification = notifications.FirstOrDefault();

        if (notification == null) return false;

        await _unitOfWork.Notifications.DeleteAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task CreateNotificationAsync(CreateNotificationDto dto)
    {
        var notification = new Notification
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
    }
}
