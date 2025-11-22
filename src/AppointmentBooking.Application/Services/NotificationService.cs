using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task<NotificationResultDto> SendAppointmentConfirmationAsync(Appointment appointment)
    {
        _logger.LogInformation("Confirmation sent for {Code}", appointment.ConfirmationCode);
        return Task.FromResult(new NotificationResultDto { Success = true, Message = "Sent" });
    }

    public Task<NotificationResultDto> SendCancellationNotificationAsync(Appointment appointment)
    {
        _logger.LogInformation("Cancellation sent for {Code}", appointment.ConfirmationCode);
        return Task.FromResult(new NotificationResultDto { Success = true, Message = "Sent" });
    }
}
