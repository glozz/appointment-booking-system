using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Application.DTOs;

/// <summary>
/// DTO for consultant self-registration
/// </summary>
public class ConsultantRegistrationDto
{
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
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [Required]
    public int BranchId { get; set; }
    
    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }
}

/// <summary>
/// DTO for pending consultant display
/// </summary>
public class PendingConsultantDto
{
    public int ConsultantId { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>
/// Result of consultant registration
/// </summary>
public class ConsultantRegistrationResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? ConsultantId { get; set; }
}
