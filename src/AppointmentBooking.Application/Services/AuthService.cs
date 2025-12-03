using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IActivityLogService _activityLogService;
    private readonly IEmailService _emailService;
    
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;
    private const int BcryptWorkFactor = 12;

    public AuthService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IActivityLogService activityLogService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        _activityLogService = activityLogService;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string? ipAddress = null, string? userAgent = null)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", dto.Email);

        // Check if email already exists
        var existingUsers = await _unitOfWork.Users.FindAsync(u => u.Email == dto.Email);
        if (existingUsers.Any())
        {
            _logger.LogWarning("Registration failed - email already exists: {Email}", dto.Email);
            return new AuthResponseDto { Success = false, Message = "Email address is already registered." };
        }

        // Validate password requirements
        if (!IsValidPassword(dto.Password))
        {
            return new AuthResponseDto 
            { 
                Success = false, 
                Message = "Password must be at least 8 characters and contain uppercase, lowercase, number, and special character." 
            };
        }

        // Create user
        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BcryptWorkFactor),
            Phone = dto.Phone,
            Role = UserRole.User,
            IsActive = true,
            EmailVerified = false,
            EmailVerificationToken = GenerateSecureToken(),
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User registered successfully: {Email}", dto.Email);

        // Log activity
        await _activityLogService.LogActivityAsync(user.Id, "User.Register", "User", user.Id, 
            "User registered", ipAddress, userAgent);

        // Send verification email
        try
        {
            var verificationLink = $"{_configuration["App:BaseUrl"]}/verify-email?token={user.EmailVerificationToken}";
            await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName, verificationLink);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
        }

        // Create session
        var (accessToken, refreshToken, expiresAt) = await CreateSessionAsync(user, ipAddress, userAgent);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Registration successful. Please verify your email.",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ipAddress = null, string? userAgent = null)
    {
        _logger.LogInformation("Login attempt for email: {Email}", dto.Email);

        var users = await _unitOfWork.Users.FindAsync(u => u.Email == dto.Email.ToLowerInvariant());
        var user = users.FirstOrDefault();

        if (user == null)
        {
            _logger.LogWarning("Login failed - user not found: {Email}", dto.Email);
            await _activityLogService.LogActivityAsync(null, "User.LoginFailed", null, null, 
                $"Invalid login attempt for email: {dto.Email}", ipAddress, userAgent);
            return new AuthResponseDto { Success = false, Message = "Invalid email or password." };
        }

        // Check if account is locked
        if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
        {
            var remainingMinutes = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
            _logger.LogWarning("Login failed - account locked: {Email}", dto.Email);
            return new AuthResponseDto 
            { 
                Success = false, 
                Message = $"Account is locked. Try again in {remainingMinutes + 1} minutes." 
            };
        }

        // Check if account is active
        if (!user.IsActive)
        {
            // Check if this is a consultant pending approval
            if (user.Role == UserRole.Consultant)
            {
                _logger.LogWarning("Login failed - consultant account pending approval: {Email}", dto.Email);
                return new AuthResponseDto { Success = false, Message = "Your account is pending admin approval. Please wait for activation." };
            }
            _logger.LogWarning("Login failed - account deactivated: {Email}", dto.Email);
            return new AuthResponseDto { Success = false, Message = "Account has been deactivated." };
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                _logger.LogWarning("Account locked due to too many failed attempts: {Email}", dto.Email);
                
                try
                {
                    await _emailService.SendAccountLockedEmailAsync(user.Email, user.FirstName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send account locked email to {Email}", user.Email);
                }
            }

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            await _activityLogService.LogActivityAsync(user.Id, "User.LoginFailed", "User", user.Id, 
                "Invalid password", ipAddress, userAgent);

            _logger.LogWarning("Login failed - invalid password: {Email}", dto.Email);
            return new AuthResponseDto { Success = false, Message = "Invalid email or password." };
        }

        // Reset failed attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Create session
        var (accessToken, refreshToken, expiresAt) = await CreateSessionAsync(user, ipAddress, userAgent, dto.RememberMe);

        await _activityLogService.LogActivityAsync(user.Id, "User.Login", "User", user.Id, 
            "User logged in", ipAddress, userAgent);

        _logger.LogInformation("User logged in successfully: {Email}", dto.Email);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful.",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        };
    }

    public async Task<bool> LogoutAsync(int userId, string? refreshToken = null)
    {
        _logger.LogInformation("Logout for user: {UserId}", userId);

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var sessions = await _unitOfWork.Sessions.FindAsync(s => 
                s.UserId == userId && s.RefreshToken == refreshToken && s.RevokedAt == null);
            var session = sessions.FirstOrDefault();

            if (session != null)
            {
                session.RevokedAt = DateTime.UtcNow;
                await _unitOfWork.Sessions.UpdateAsync(session);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        await _activityLogService.LogActivityAsync(userId, "User.Logout", "User", userId, "User logged out");
        return true;
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null)
    {
        var sessions = await _unitOfWork.Sessions.FindAsync(s => s.RefreshToken == refreshToken);
        var session = sessions.FirstOrDefault();

        if (session == null || !session.IsActive)
        {
            return new AuthResponseDto { Success = false, Message = "Invalid or expired refresh token." };
        }

        var users = await _unitOfWork.Users.FindAsync(u => u.Id == session.UserId);
        var user = users.FirstOrDefault();

        if (user == null || !user.IsActive)
        {
            return new AuthResponseDto { Success = false, Message = "User not found or inactive." };
        }

        // Revoke old session
        session.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.Sessions.UpdateAsync(session);

        // Create new session
        var (accessToken, newRefreshToken, expiresAt) = await CreateSessionAsync(user, ipAddress, userAgent);

        return new AuthResponseDto
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        };
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var users = await _unitOfWork.Users.FindAsync(u => 
            u.EmailVerificationToken == token && 
            u.EmailVerificationTokenExpiry > DateTime.UtcNow);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            _logger.LogWarning("Email verification failed - invalid or expired token");
            return false;
        }

        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(user.Id, "User.EmailVerified", "User", user.Id, "Email verified");

        _logger.LogInformation("Email verified for user: {Email}", user.Email);
        return true;
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var users = await _unitOfWork.Users.FindAsync(u => u.Email == email.ToLowerInvariant());
        var user = users.FirstOrDefault();

        // Always return true to prevent email enumeration
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return true;
        }

        user.PasswordResetToken = GenerateSecureToken();
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            var resetLink = $"{_configuration["App:BaseUrl"]}/reset-password?token={user.PasswordResetToken}";
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName, resetLink);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
        }

        await _activityLogService.LogActivityAsync(user.Id, "User.PasswordResetRequested", "User", user.Id, 
            "Password reset requested");

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var users = await _unitOfWork.Users.FindAsync(u => 
            u.PasswordResetToken == dto.Token && 
            u.PasswordResetTokenExpiry > DateTime.UtcNow);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            _logger.LogWarning("Password reset failed - invalid or expired token");
            return false;
        }

        if (!IsValidPassword(dto.Password))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BcryptWorkFactor);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Revoke all sessions
        await RevokeAllSessionsAsync(user.Id);

        try
        {
            await _emailService.SendPasswordChangedEmailAsync(user.Email, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password changed email to {Email}", user.Email);
        }

        await _activityLogService.LogActivityAsync(user.Id, "User.PasswordReset", "User", user.Id, 
            "Password reset completed");

        _logger.LogInformation("Password reset completed for user: {Email}", user.Email);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        if (!IsValidPassword(dto.NewPassword))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, BcryptWorkFactor);
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            await _emailService.SendPasswordChangedEmailAsync(user.Email, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password changed email to {Email}", user.Email);
        }

        await _activityLogService.LogActivityAsync(userId, "User.PasswordChanged", "User", userId, 
            "Password changed");

        _logger.LogInformation("Password changed for user: {UserId}", userId);
        return true;
    }

    public async Task<bool> RevokeAllSessionsAsync(int userId, int? exceptSessionId = null)
    {
        var sessions = await _unitOfWork.Sessions.FindAsync(s => 
            s.UserId == userId && s.RevokedAt == null);

        foreach (var session in sessions)
        {
            if (exceptSessionId.HasValue && session.Id == exceptSessionId.Value)
                continue;

            session.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.Sessions.UpdateAsync(session);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<(string accessToken, string refreshToken, DateTime expiresAt)> CreateSessionAsync(
        User user, string? ipAddress, string? userAgent, bool rememberMe = false)
    {
        var refreshToken = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddDays(rememberMe ? 30 : 7);

        var session = new Session
        {
            UserId = user.Id,
            RefreshToken = refreshToken,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        await _unitOfWork.Sessions.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        // Generate access token with session ID
        var accessToken = GenerateAccessToken(user, session.Id);

        return (accessToken, refreshToken, expiresAt);
    }

    private string GenerateAccessToken(User user, int sessionId = 0)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "AppointmentBooking";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "AppointmentBooking";
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
            new Claim("sessionId", sessionId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[64];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static bool IsValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Phone = user.Phone,
        ProfilePicture = user.ProfilePicture,
        Role = user.Role.ToString(),
        IsActive = user.IsActive,
        EmailVerified = user.EmailVerified,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt
    };
}
