using System.ComponentModel.DataAnnotations;
using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Application.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationDto
{
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    public NotificationType Type { get; set; } = NotificationType.Info;
}

public class AppointmentTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public string Color { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
