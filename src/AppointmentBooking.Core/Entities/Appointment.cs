using System.ComponentModel.DataAnnotations;
using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Core.Entities;

public class Appointment
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string ConfirmationCode { get; set; } = string.Empty;
    
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    
    public int? ConsultantId { get; set; }
    public Consultant? Consultant { get; set; }
    
    [Required]
    public DateTime AppointmentDate { get; set; }
    
    [Required]
    public TimeSpan StartTime { get; set; }
    
    [Required]
    public TimeSpan EndTime { get; set; }
    
    [Required]
    public AppointmentStatus Status { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    [StringLength(500)]
    public string? CancellationReason { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
