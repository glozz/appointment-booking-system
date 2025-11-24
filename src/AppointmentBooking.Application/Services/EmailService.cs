using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.Interfaces;

namespace AppointmentBooking.Application.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        // In production, this would connect to an SMTP server or email service
        _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        return Task.CompletedTask;
    }

    public async Task SendWelcomeEmailAsync(string to, string firstName, string verificationLink)
    {
        var subject = "Welcome to Appointment Booking System";
        var body = GetWelcomeEmailTemplate(firstName, verificationLink);
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendVerificationEmailAsync(string to, string firstName, string verificationLink)
    {
        var subject = "Verify Your Email Address";
        var body = GetVerificationEmailTemplate(firstName, verificationLink);
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string to, string firstName, string resetLink)
    {
        var subject = "Reset Your Password";
        var body = GetPasswordResetEmailTemplate(firstName, resetLink);
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendPasswordChangedEmailAsync(string to, string firstName)
    {
        var subject = "Your Password Has Been Changed";
        var body = GetPasswordChangedEmailTemplate(firstName);
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendAppointmentConfirmationEmailAsync(string to, string firstName, string appointmentDetails)
    {
        var subject = "Appointment Confirmed";
        var body = GetAppointmentConfirmationEmailTemplate(firstName, appointmentDetails);
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendAppointmentReminderEmailAsync(string to, string firstName, string appointmentDetails)
    {
        var subject = "Appointment Reminder";
        var body = GetAppointmentReminderEmailTemplate(firstName, appointmentDetails);
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendAppointmentCancellationEmailAsync(string to, string firstName, string appointmentDetails)
    {
        var subject = "Appointment Cancelled";
        var body = GetAppointmentCancellationEmailTemplate(firstName, appointmentDetails);
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendAccountLockedEmailAsync(string to, string firstName)
    {
        var subject = "Account Security Alert - Account Locked";
        var body = GetAccountLockedEmailTemplate(firstName);
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendAccountUnlockedEmailAsync(string to, string firstName)
    {
        var subject = "Account Unlocked";
        var body = GetAccountUnlockedEmailTemplate(firstName);
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendAccountDeactivatedEmailAsync(string to, string firstName)
    {
        var subject = "Account Deactivated";
        var body = GetAccountDeactivatedEmailTemplate(firstName);
        await SendEmailAsync(to, subject, body);
    }

    private static string GetEmailWrapper(string title, string content)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ background: white; padding: 30px; border-radius: 0 0 10px 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .button {{ display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 14px 28px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold; }}
        .button:hover {{ opacity: 0.9; }}
        .footer {{ text-align: center; padding: 20px; color: #888; font-size: 12px; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffc107; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .info {{ background-color: #d1ecf1; border: 1px solid #0dcaf0; padding: 15px; border-radius: 5px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Appointment Booking System</h1>
        </div>
        <div class=""content"">
            {content}
        </div>
        <div class=""footer"">
            <p>&copy; {DateTime.UtcNow.Year} Appointment Booking System. All rights reserved.</p>
            <p>This is an automated message. Please do not reply directly to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GetWelcomeEmailTemplate(string firstName, string verificationLink)
    {
        var content = $@"
            <h2>Welcome, {firstName}!</h2>
            <p>Thank you for creating an account with our Appointment Booking System. We're excited to have you on board!</p>
            <p>To get started, please verify your email address by clicking the button below:</p>
            <p style=""text-align: center;"">
                <a href=""{verificationLink}"" class=""button"">Verify Email Address</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #667eea;"">{verificationLink}</p>
            <div class=""info"">
                <p><strong>This verification link will expire in 24 hours.</strong></p>
            </div>
            <p>Once verified, you'll be able to:</p>
            <ul>
                <li>Book appointments at your convenience</li>
                <li>Manage your upcoming appointments</li>
                <li>Receive reminders and notifications</li>
            </ul>
            <p>If you didn't create this account, please ignore this email.</p>
            <p>Best regards,<br>The Appointment Booking Team</p>";
        
        return GetEmailWrapper("Welcome to Appointment Booking", content);
    }

    private static string GetVerificationEmailTemplate(string firstName, string verificationLink)
    {
        var content = $@"
            <h2>Verify Your Email</h2>
            <p>Hi {firstName},</p>
            <p>Please verify your email address by clicking the button below:</p>
            <p style=""text-align: center;"">
                <a href=""{verificationLink}"" class=""button"">Verify Email Address</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #667eea;"">{verificationLink}</p>
            <div class=""info"">
                <p><strong>This verification link will expire in 24 hours.</strong></p>
            </div>
            <p>If you didn't request this verification, please ignore this email.</p>
            <p>Best regards,<br>The Appointment Booking Team</p>";
        
        return GetEmailWrapper("Verify Your Email", content);
    }

    private static string GetPasswordResetEmailTemplate(string firstName, string resetLink)
    {
        var content = $@"
            <h2>Password Reset Request</h2>
            <p>Hi {firstName},</p>
            <p>We received a request to reset your password. Click the button below to create a new password:</p>
            <p style=""text-align: center;"">
                <a href=""{resetLink}"" class=""button"">Reset Password</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #667eea;"">{resetLink}</p>
            <div class=""warning"">
                <p><strong>This link will expire in 1 hour.</strong></p>
                <p>If you didn't request this password reset, please ignore this email or contact support if you have concerns.</p>
            </div>
            <p>Best regards,<br>The Appointment Booking Team</p>";
        
        return GetEmailWrapper("Reset Your Password", content);
    }

    private static string GetPasswordChangedEmailTemplate(string firstName)
    {
        var content = $@"
            <h2>Password Changed Successfully</h2>
            <p>Hi {firstName},</p>
            <p>Your password has been successfully changed.</p>
            <div class=""warning"">
                <p><strong>If you didn't make this change, please contact support immediately or reset your password.</strong></p>
            </div>
            <p>For your security, you may have been logged out of other devices.</p>
            <p>Best regards,<br>The Appointment Booking Team</p>";
        
        return GetEmailWrapper("Password Changed", content);
    }

    private static string GetAppointmentConfirmationEmailTemplate(string firstName, string appointmentDetails)
    {
        var content = $@"
            <h2>Appointment Confirmed!</h2>
            <p>Hi {firstName},</p>
            <p>Your appointment has been successfully booked. Here are the details:</p>
            <div class=""info"">
                {appointmentDetails}
            </div>
            <p>Please arrive 5-10 minutes before your scheduled time.</p>
            <p>Need to make changes? Log in to your account to reschedule or cancel your appointment.</p>
            <p>Best regards,<br>The Appointment Booking Team</p>";
        
        return GetEmailWrapper("Appointment Confirmed", content);
    }

    private static string GetAppointmentReminderEmailTemplate(string firstName, string appointmentDetails)
    {
        var content = $@"
            <h2>Appointment Reminder</h2>
            <p>Hi {firstName},</p>
            <p>This is a friendly reminder about your upcoming appointment:</p>
            <div class=""info"">
                {appointmentDetails}
            </div>
            <p>Please arrive 5-10 minutes before your scheduled time.</p>
            <p>Need to make changes? Log in to your account to reschedule or cancel your appointment.</p>
            <p>Best regards,<br>The Appointment Booking Team</p>";
        
        return GetEmailWrapper("Appointment Reminder", content);
    }

    private static string GetAppointmentCancellationEmailTemplate(string firstName, string appointmentDetails)
    {
        var content = $@"
            <h2>Appointment Cancelled</h2>
            <p>Hi {firstName},</p>
            <p>Your appointment has been cancelled. Here are the details of the cancelled appointment:</p>
            <div class=""info"">
                {appointmentDetails}
            </div>
            <p>If you'd like to book a new appointment, please log in to your account.</p>
            <p>Best regards,<br>The Appointment Booking Team</p>";
        
        return GetEmailWrapper("Appointment Cancelled", content);
    }

    private static string GetAccountLockedEmailTemplate(string firstName)
    {
        var content = $@"
            <h2>Account Security Alert</h2>
            <p>Hi {firstName},</p>
            <div class=""warning"">
                <p><strong>Your account has been temporarily locked due to multiple failed login attempts.</strong></p>
            </div>
            <p>For your security, we've locked your account for 15 minutes. After this time, you can try logging in again.</p>
            <p>If this was you, please wait and try again later with the correct password.</p>
            <p>If you've forgotten your password, you can reset it after the lockout period.</p>
            <p><strong>If this wasn't you</strong>, someone may be trying to access your account. We recommend:</p>
            <ul>
                <li>Changing your password immediately after the lockout period</li>
                <li>Using a strong, unique password</li>
                <li>Contacting support if you need assistance</li>
            </ul>
            <p>Best regards,<br>The Appointment Booking Team</p>";
        
        return GetEmailWrapper("Account Locked", content);
    }

    private static string GetAccountUnlockedEmailTemplate(string firstName)
    {
        var content = $@"
            <h2>Account Unlocked</h2>
            <p>Hi {firstName},</p>
            <p>Good news! Your account has been unlocked and you can now log in.</p>
            <p>If you were locked out due to forgotten password, we recommend resetting your password to something memorable but secure.</p>
            <p>Best regards,<br>The Appointment Booking Team</p>";
        
        return GetEmailWrapper("Account Unlocked", content);
    }

    private static string GetAccountDeactivatedEmailTemplate(string firstName)
    {
        var content = $@"
            <h2>Account Deactivated</h2>
            <p>Hi {firstName},</p>
            <p>Your account has been deactivated. You will no longer be able to log in or access our services.</p>
            <p>If you believe this was done in error or would like to reactivate your account, please contact our support team.</p>
            <p>Best regards,<br>The Appointment Booking Team</p>";
        
        return GetEmailWrapper("Account Deactivated", content);
    }
}
