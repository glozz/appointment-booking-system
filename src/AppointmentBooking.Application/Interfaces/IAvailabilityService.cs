using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Application.Interfaces;

public interface IAvailabilityService
{
    Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(int branchId, int serviceId, DateTime date);
    Task<IEnumerable<DateTime>> GetAvailableDatesAsync(int branchId, int daysAhead = 30);
    Task<bool> IsSlotAvailableAsync(int branchId, DateTime date, TimeSpan startTime, TimeSpan endTime);
}
