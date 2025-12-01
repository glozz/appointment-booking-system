using AppointmentBooking.Core.Enums;

namespace AppointmentBooking.Application.DTOs;

/// <summary>
/// Lightweight DTO for appointment list views
/// </summary>
public class AppointmentSummaryDto
{
    public int Id { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? ConsultantName { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public AppointmentStatus Status { get; set; }
}
