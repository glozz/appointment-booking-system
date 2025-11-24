using System.ComponentModel.DataAnnotations;
using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Application.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ProfilePicture { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ProfilePicture { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ActiveSessionsCount { get; set; }
}

public class UpdateProfileDto
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }
}

public class SessionDto
{
    public int Id { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsCurrentSession { get; set; }
}

public class LoginHistoryDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateUserRoleDto
{
    [Required]
    public UserRole Role { get; set; }
}

public class AdminUpdateUserDto
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }
    
    public bool IsActive { get; set; }
    
    public UserRole Role { get; set; }
}
