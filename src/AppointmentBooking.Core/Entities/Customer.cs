using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.Entities;

public class Customer
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
