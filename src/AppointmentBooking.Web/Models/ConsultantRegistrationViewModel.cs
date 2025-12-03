using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppointmentBooking.Web.Models;

public class ConsultantRegistrationViewModel
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Branch is required")]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    public List<SelectListItem> Branches { get; set; } = new();
}
