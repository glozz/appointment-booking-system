using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string? ipAddress = null, string? userAgent = null);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, string? ipAddress = null, string? userAgent = null);
    Task<bool> LogoutAsync(int userId, string? refreshToken = null);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<bool> RevokeAllSessionsAsync(int userId, int? exceptSessionId = null);
}
