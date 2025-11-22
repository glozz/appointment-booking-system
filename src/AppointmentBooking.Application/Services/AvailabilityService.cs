using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Application.Services;

/// <summary>
/// Service for checking appointment slot availability
/// </summary>
public class AvailabilityService : IAvailabilityService
{
    private readonly IUnitOfWork _unitOfWork;

    public AvailabilityService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Get all available time slots for a specific branch, service, and date
    /// </summary>
    public async Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(int branchId, int serviceId, DateTime date)
    {
        // Get service details to determine slot duration
        var service = await _unitOfWork.Services.GetByIdAsync(serviceId);
        if (service == null)
            return Enumerable.Empty<AvailableSlotDto>();

        // Get branch operating hours for the day
        var operatingHours = await _unitOfWork.BranchOperatingHours.FindAsync(
            h => h.BranchId == branchId && h.DayOfWeek == date.DayOfWeek);
        
        var hours = operatingHours.FirstOrDefault();
        if (hours == null || hours.IsClosed)
            return Enumerable.Empty<AvailableSlotDto>();

        // Generate time slots based on operating hours
        var slots = new List<AvailableSlotDto>();
        var slotDuration = TimeSpan.FromMinutes(service.DurationMinutes);
        var currentTime = hours.OpenTime;

        // Create 30-minute interval slots
        while (currentTime.Add(slotDuration) <= hours.CloseTime)
        {
            var endTime = currentTime.Add(slotDuration);
            var isAvailable = await IsSlotAvailableAsync(branchId, date, currentTime, endTime);

            slots.Add(new AvailableSlotDto
            {
                Date = date,
                StartTime = currentTime,
                EndTime = endTime,
                IsAvailable = isAvailable
            });

            currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
        }

        return slots;
    }

    /// <summary>
    /// Get list of dates when branch is open (next X days)
    /// </summary>
    public async Task<IEnumerable<DateTime>> GetAvailableDatesAsync(int branchId, int daysAhead = 30)
    {
        var dates = new List<DateTime>();
        var currentDate = DateTime.Today;

        for (int i = 1; i <= daysAhead; i++)
        {
            var date = currentDate.AddDays(i);
            var operatingHours = await _unitOfWork.BranchOperatingHours.FindAsync(
                h => h.BranchId == branchId && h.DayOfWeek == date.DayOfWeek);
            
            var hours = operatingHours.FirstOrDefault();
            if (hours != null && !hours.IsClosed)
            {
                dates.Add(date);
            }
        }

        return dates;
    }

    /// <summary>
    /// Check if a specific time slot is available (no conflicts, minimum lead time)
    /// </summary>
    public async Task<bool> IsSlotAvailableAsync(int branchId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        // Check minimum lead time (1 hour in advance)
        var slotDateTime = date.Add(startTime);
        if (slotDateTime <= DateTime.Now.AddHours(1))
            return false;

        // Check for conflicting appointments
        var appointments = await _unitOfWork.Appointments.FindAsync(a =>
            a.BranchId == branchId &&
            a.AppointmentDate == date &&
            a.Status != AppointmentStatus.Cancelled &&
            (a.StartTime < endTime && a.EndTime > startTime)); // Overlapping time check

        return !appointments.Any();
    }
}
