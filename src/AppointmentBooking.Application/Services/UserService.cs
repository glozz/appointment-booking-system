using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IActivityLogService _activityLogService;
    private readonly IEmailService _emailService;

    public UserService(
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger,
        IActivityLogService activityLogService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _activityLogService = activityLogService;
        _emailService = emailService;
    }

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return null;

        var activeSessions = await _unitOfWork.Sessions.FindAsync(s => 
            s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow);

        return new UserProfileDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            ProfilePicture = user.ProfilePicture,
            Role = user.Role.ToString(),
            EmailVerified = user.EmailVerified,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            ActiveSessionsCount = activeSessions.Count()
        };
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return null;

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Phone = dto.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(userId, "User.ProfileUpdated", "User", userId, 
            "Profile updated");

        return await GetProfileAsync(userId);
    }

    public async Task<IEnumerable<SessionDto>> GetActiveSessionsAsync(int userId, int currentSessionId)
    {
        var sessions = await _unitOfWork.Sessions.FindAsync(s => 
            s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow);

        return sessions.Select(s => new SessionDto
        {
            Id = s.Id,
            IpAddress = s.IpAddress,
            UserAgent = s.UserAgent,
            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt,
            IsCurrentSession = s.Id == currentSessionId
        }).OrderByDescending(s => s.CreatedAt);
    }

    public async Task<bool> RevokeSessionAsync(int userId, int sessionId)
    {
        var sessions = await _unitOfWork.Sessions.FindAsync(s => 
            s.Id == sessionId && s.UserId == userId && s.RevokedAt == null);
        var session = sessions.FirstOrDefault();

        if (session == null) return false;

        session.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.Sessions.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(userId, "User.SessionRevoked", "Session", sessionId, 
            "Session revoked");

        return true;
    }

    public async Task<IEnumerable<LoginHistoryDto>> GetLoginHistoryAsync(int userId, int limit = 20)
    {
        var activities = await _unitOfWork.ActivityLogs.FindAsync(a => 
            a.UserId == userId && 
            (a.Action == "User.Login" || a.Action == "User.LoginFailed" || a.Action == "User.Logout"));

        return activities
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new LoginHistoryDto
            {
                Id = a.Id,
                Action = a.Action,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                CreatedAt = a.CreatedAt
            });
    }

    // Admin operations
    public async Task<PaginatedResultDto<AdminUserDto>> GetUsersAsync(UserSearchDto search)
    {
        var allUsers = await _unitOfWork.Users.GetAllAsync();
        var query = allUsers.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            var searchLower = search.Search.ToLowerInvariant();
            query = query.Where(u => 
                u.FirstName.ToLower().Contains(searchLower) ||
                u.LastName.ToLower().Contains(searchLower) ||
                u.Email.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrWhiteSpace(search.Role) && Enum.TryParse<UserRole>(search.Role, true, out var role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (search.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == search.IsActive.Value);
        }

        var totalCount = query.Count();

        // Apply sorting
        query = search.SortBy?.ToLower() switch
        {
            "firstname" => search.SortDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
            "lastname" => search.SortDescending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
            "email" => search.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "lastloginat" => search.SortDescending ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
            _ => search.SortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
        };

        // Apply pagination
        var users = query
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                EmailVerified = u.EmailVerified,
                FailedLoginAttempts = u.FailedLoginAttempts,
                LockoutEnd = u.LockoutEnd,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            })
            .ToList();

        return new PaginatedResultDto<AdminUserDto>
        {
            Items = users,
            TotalCount = totalCount,
            Page = search.Page,
            PageSize = search.PageSize
        };
    }

    public async Task<AdminUserDto?> GetUserByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return null;

        return new AdminUserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            EmailVerified = user.EmailVerified,
            FailedLoginAttempts = user.FailedLoginAttempts,
            LockoutEnd = user.LockoutEnd,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<AdminUserDto?> UpdateUserAsync(int id, AdminUpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return null;

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Phone = dto.Phone;
        user.IsActive = dto.IsActive;
        user.Role = dto.Role;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(null, "Admin.UserUpdated", "User", id, 
            $"User updated by admin");

        return await GetUserByIdAsync(id);
    }

    public async Task<bool> ActivateUserAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return false;

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(null, "Admin.UserActivated", "User", id, 
            $"User activated by admin");

        return true;
    }

    public async Task<bool> DeactivateUserAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            await _emailService.SendAccountDeactivatedEmailAsync(user.Email, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send account deactivated email to {Email}", user.Email);
        }

        await _activityLogService.LogActivityAsync(null, "Admin.UserDeactivated", "User", id, 
            $"User deactivated by admin");

        return true;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return false;

        // Soft delete
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(null, "Admin.UserDeleted", "User", id, 
            $"User soft-deleted by admin");

        return true;
    }

    public async Task<bool> UpdateUserRoleAsync(int id, UserRole role)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return false;

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(null, "Admin.UserRoleChanged", "User", id, 
            $"User role changed to {role}");

        return true;
    }

    public async Task<bool> UnlockUserAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return false;

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            await _emailService.SendAccountUnlockedEmailAsync(user.Email, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send account unlocked email to {Email}", user.Email);
        }

        await _activityLogService.LogActivityAsync(null, "Admin.UserUnlocked", "User", id, 
            $"User unlocked by admin");

        return true;
    }

    public async Task<IEnumerable<LoginHistoryDto>> GetUserActivityAsync(int userId, int limit = 50)
    {
        var activities = await _unitOfWork.ActivityLogs.FindAsync(a => a.UserId == userId);

        return activities
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new LoginHistoryDto
            {
                Id = a.Id,
                Action = a.Action,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                CreatedAt = a.CreatedAt
            });
    }
}
