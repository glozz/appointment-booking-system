using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.Models;

/// <summary>
/// View model for the consultant dashboard
/// </summary>
public class ConsultantDashboardViewModel
{
    public ConsultantDto? Consultant { get; set; }
    public DateTime Date { get; set; }
    public IEnumerable<AppointmentDto> TodayAppointments { get; set; } = Enumerable.Empty<AppointmentDto>();
    public int TotalToday { get; set; }
    public int CompletedToday { get; set; }
    public int RemainingToday { get; set; }
    public IList<TimeSlotViewModel> TimeSlots { get; set; } = new List<TimeSlotViewModel>();
}

/// <summary>
/// Represents a time slot in the consultant's schedule
/// </summary>
public class TimeSlotViewModel
{
    public TimeSpan Time { get; set; }
    public bool IsBooked { get; set; }
    public AppointmentDto? Appointment { get; set; }
}
