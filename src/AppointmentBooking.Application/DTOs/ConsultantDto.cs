namespace AppointmentBooking.Application.DTOs;

public class ConsultantDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
}
