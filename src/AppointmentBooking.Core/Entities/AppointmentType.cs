using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.Entities;

public class AppointmentType
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [Range(5, 480)]
    public int DurationMinutes { get; set; }
    
    [StringLength(7)]
    public string Color { get; set; } = "#3B82F6";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}
