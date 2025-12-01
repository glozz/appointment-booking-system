using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.Entities;

public class Consultant
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    
    /// <summary>
    /// Optional link to a User account for authentication purposes
    /// </summary>
    public int? UserId { get; set; }
    public User? User { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
