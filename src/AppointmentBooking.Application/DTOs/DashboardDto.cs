namespace AppointmentBooking.Application.DTOs;

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalAppointments { get; set; }
    public int UpcomingAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int TodayAppointments { get; set; }
    public int NewUsersThisMonth { get; set; }
    public List<RecentActivityDto> RecentActivity { get; set; } = new();
}

public class RecentActivityDto
{
    public string Action { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDashboardDto
{
    public int UpcomingAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int UnreadNotifications { get; set; }
    public UserAppointmentDto? NextAppointment { get; set; }
    public List<UserAppointmentDto> RecentAppointments { get; set; } = new();
}

public class UserAppointmentDto
{
    public int Id { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? BranchName { get; set; }
    public string? ServiceName { get; set; }
    public string? CustomerName { get; set; }
}

public class PaginatedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class UserSearchDto
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}
