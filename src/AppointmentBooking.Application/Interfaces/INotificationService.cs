using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Core.Entities;

namespace AppointmentBooking.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationResultDto> SendAppointmentConfirmationAsync(Appointment appointment);
    Task<NotificationResultDto> SendCancellationNotificationAsync(Appointment appointment);
}
