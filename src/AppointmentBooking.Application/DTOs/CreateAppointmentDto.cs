namespace AppointmentBooking.Application.DTOs;

public class CreateAppointmentDto
{
    public int BranchId { get; set; }
    public int ServiceId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public CustomerDto Customer { get; set; } = null!;
    public string? Notes { get; set; }
}
