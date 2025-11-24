using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Application.Interfaces;

public interface IUserNotificationService
{
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int userId, int notificationId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<bool> DeleteNotificationAsync(int userId, int notificationId);
    Task CreateNotificationAsync(CreateNotificationDto dto);
}
