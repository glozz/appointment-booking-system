using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetAdminDashboardStatsAsync();
    Task<UserDashboardDto> GetUserDashboardAsync(int userId);
}
