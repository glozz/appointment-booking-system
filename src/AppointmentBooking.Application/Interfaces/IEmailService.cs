namespace AppointmentBooking.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendWelcomeEmailAsync(string to, string firstName, string verificationLink);
    Task SendVerificationEmailAsync(string to, string firstName, string verificationLink);
    Task SendPasswordResetEmailAsync(string to, string firstName, string resetLink);
    Task SendPasswordChangedEmailAsync(string to, string firstName);
    Task SendAppointmentConfirmationEmailAsync(string to, string firstName, string appointmentDetails);
    Task SendAppointmentReminderEmailAsync(string to, string firstName, string appointmentDetails);
    Task SendAppointmentCancellationEmailAsync(string to, string firstName, string appointmentDetails);
    Task SendAccountLockedEmailAsync(string to, string firstName);
    Task SendAccountUnlockedEmailAsync(string to, string firstName);
    Task SendAccountDeactivatedEmailAsync(string to, string firstName);
}
