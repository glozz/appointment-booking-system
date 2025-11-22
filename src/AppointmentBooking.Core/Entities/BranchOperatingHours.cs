using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.Entities;

public class BranchOperatingHours
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    
    [Required]
    public TimeSpan OpenTime { get; set; }
    
    [Required]
    public TimeSpan CloseTime { get; set; }
    
    public bool IsClosed { get; set; }
}
