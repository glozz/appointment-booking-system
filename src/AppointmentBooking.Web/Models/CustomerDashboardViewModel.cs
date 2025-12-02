using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Web.Models;

/// <summary>
/// View model for the customer dashboard
/// </summary>
public class CustomerDashboardViewModel
{
    public IEnumerable<AppointmentDto> UpcomingAppointments { get; set; } = Enumerable.Empty<AppointmentDto>();
    public IEnumerable<AppointmentDto> PastAppointments { get; set; } = Enumerable.Empty<AppointmentDto>();
    public int TotalAppointments { get; set; }
    public int UpcomingCount { get; set; }
    public int CompletedCount { get; set; }
}
