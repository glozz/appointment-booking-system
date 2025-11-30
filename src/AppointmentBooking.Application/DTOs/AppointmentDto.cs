using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Application.DTOs;

public class AppointmentDto
{
    public int Id { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;
    public CustomerDto Customer { get; set; } = null!;
    public BranchDto Branch { get; set; } = null!;
    public ServiceDto Service { get; set; } = null!;
    public ConsultantDto? Consultant { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
