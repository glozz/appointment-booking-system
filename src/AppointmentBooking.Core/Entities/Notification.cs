using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.Entities;

public class Notification
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    public NotificationType Type { get; set; }
    
    public bool IsRead { get; set; } = false;
    
    public DateTime? ReadAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

public enum NotificationType
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    AppointmentReminder = 4,
    AppointmentConfirmation = 5,
    AppointmentCancellation = 6,
    AccountAlert = 7
}
