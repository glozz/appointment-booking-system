using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Application.Interfaces;

public interface IUserService
{
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<IEnumerable<SessionDto>> GetActiveSessionsAsync(int userId, int currentSessionId);
    Task<bool> RevokeSessionAsync(int userId, int sessionId);
    Task<IEnumerable<LoginHistoryDto>> GetLoginHistoryAsync(int userId, int limit = 20);
    
    // Admin operations
    Task<PaginatedResultDto<AdminUserDto>> GetUsersAsync(UserSearchDto search);
    Task<AdminUserDto?> GetUserByIdAsync(int id);
    Task<AdminUserDto?> UpdateUserAsync(int id, AdminUpdateUserDto dto);
    Task<bool> ActivateUserAsync(int id);
    Task<bool> DeactivateUserAsync(int id);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> UpdateUserRoleAsync(int id, UserRole role);
    Task<bool> UnlockUserAsync(int id);
    Task<IEnumerable<LoginHistoryDto>> GetUserActivityAsync(int userId, int limit = 50);
}
