using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Application.DTOs;

public class CustomerDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [RegularExpression(@"^(0\d{9}|\+27\d{9})$", ErrorMessage = "Enter a valid South African number (084xxxxxxx or +27xxxxxxxxx)")]
    public string Phone { get; set; } = string.Empty;
}
