using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.Entities;

public class Branch
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public ICollection<BranchOperatingHours> OperatingHours { get; set; } = new List<BranchOperatingHours>();
    public ICollection<BranchService> BranchServices { get; set; } = new List<BranchService>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Consultant> Consultants { get; set; } = new List<Consultant>();
}
