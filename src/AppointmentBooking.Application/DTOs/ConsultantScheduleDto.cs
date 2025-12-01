namespace AppointmentBooking.Application.DTOs;

/// <summary>
/// Day view for consultants with time slots
/// </summary>
public class ConsultantScheduleDto
{
    public int ConsultantId { get; set; }
    public string ConsultantName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public IList<TimeSlotDto> TimeSlots { get; set; } = new List<TimeSlotDto>();
}

/// <summary>
/// Represents a time slot in a consultant's schedule
/// </summary>
public class TimeSlotDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsBooked { get; set; }
    public AppointmentSummaryDto? Appointment { get; set; }
}
