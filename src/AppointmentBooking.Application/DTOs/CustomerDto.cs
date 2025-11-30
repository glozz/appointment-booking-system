using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Application.DTOs;

public class CustomerDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Phone must start with 0 and be 10–11 digits.")]
    public string Phone { get; set; } = string.Empty;
}
