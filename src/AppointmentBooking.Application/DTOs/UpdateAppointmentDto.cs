using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Application.DTOs;

public class UpdateAppointmentDto
{
    public int Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
}
