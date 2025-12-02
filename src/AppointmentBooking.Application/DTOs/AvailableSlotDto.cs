namespace AppointmentBooking.Application.DTOs;

public class AvailableSlotDto
{
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }
    /// <summary>
    /// Number of consultants available for this time slot
    /// </summary>
    public int AvailableConsultantCount { get; set; }
    /// <summary>
    /// Total number of consultants at the branch
    /// </summary>
    public int TotalConsultantCount { get; set; }
}
