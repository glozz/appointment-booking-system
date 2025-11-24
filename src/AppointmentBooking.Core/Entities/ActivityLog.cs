using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.Entities;

public class ActivityLog
{
    public int Id { get; set; }
    
    public int? UserId { get; set; }
    public User? User { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? EntityType { get; set; }
    
    public int? EntityId { get; set; }
    
    [StringLength(2000)]
    public string? Details { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
