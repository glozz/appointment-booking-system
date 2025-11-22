using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.Entities;

public class Service
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [Range(15, 240)]
    public int DurationMinutes { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public ICollection<BranchService> BranchServices { get; set; } = new List<BranchService>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
