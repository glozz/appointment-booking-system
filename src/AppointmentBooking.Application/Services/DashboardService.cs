using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IUnitOfWork unitOfWork, ILogger<DashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetAdminDashboardStatsAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var appointments = await _unitOfWork.Appointments.GetAllAsync();
        var activities = await _unitOfWork.ActivityLogs.GetAllAsync();

        var today = DateTime.UtcNow.Date;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

        return new DashboardStatsDto
        {
            TotalUsers = users.Count(),
            ActiveUsers = users.Count(u => u.IsActive),
            TotalAppointments = appointments.Count(),
            UpcomingAppointments = appointments.Count(a => 
                a.AppointmentDate >= today && 
                a.Status == AppointmentStatus.Confirmed),
            CompletedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Completed),
            CancelledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
            TodayAppointments = appointments.Count(a => a.AppointmentDate.Date == today),
            NewUsersThisMonth = users.Count(u => u.CreatedAt >= firstDayOfMonth),
            RecentActivity = activities
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new RecentActivityDto
                {
                    Action = a.Action,
                    UserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : null,
                    Details = a.Details,
                    CreatedAt = a.CreatedAt
                })
                .ToList()
        };
    }

    public async Task<UserDashboardDto> GetUserDashboardAsync(int userId)
    {
        var appointments = await _unitOfWork.Appointments.GetAllAsync();
        var notifications = await _unitOfWork.Notifications.FindAsync(n => 
            n.UserId == userId && !n.IsRead);

        // Get customer by user's email
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new UserDashboardDto();
        }

        var customers = await _unitOfWork.Customers.FindAsync(c => c.Email == user.Email);
        var customer = customers.FirstOrDefault();

        var userAppointments = customer != null 
            ? appointments.Where(a => a.CustomerId == customer.Id).ToList()
            : new List<Core.Entities.Appointment>();

        var today = DateTime.UtcNow.Date;

        var nextAppointment = userAppointments
            .Where(a => a.AppointmentDate >= today && a.Status == AppointmentStatus.Confirmed)
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .FirstOrDefault();

        return new UserDashboardDto
        {
            UpcomingAppointments = userAppointments.Count(a => 
                a.AppointmentDate >= today && a.Status == AppointmentStatus.Confirmed),
            CompletedAppointments = userAppointments.Count(a => a.Status == AppointmentStatus.Completed),
            CancelledAppointments = userAppointments.Count(a => a.Status == AppointmentStatus.Cancelled),
            UnreadNotifications = notifications.Count(),
            NextAppointment = nextAppointment != null ? MapToUserAppointmentDto(nextAppointment) : null,
            RecentAppointments = userAppointments
                .OrderByDescending(a => a.AppointmentDate)
                .Take(5)
                .Select(MapToUserAppointmentDto)
                .ToList()
        };
    }

    private static UserAppointmentDto MapToUserAppointmentDto(Core.Entities.Appointment appointment)
    {
        return new UserAppointmentDto
        {
            Id = appointment.Id,
            ConfirmationCode = appointment.ConfirmationCode,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status.ToString(),
            Notes = appointment.Notes,
            CancellationReason = appointment.CancellationReason,
            CreatedAt = appointment.CreatedAt,
            BranchName = appointment.Branch?.Name,
            ServiceName = appointment.Service?.Name,
            CustomerName = appointment.Customer != null 
                ? $"{appointment.Customer.FirstName} {appointment.Customer.LastName}" 
                : null
        };
    }
}
