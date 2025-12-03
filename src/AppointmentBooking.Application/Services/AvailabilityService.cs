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

        // Get all consultants for this branch
        var consultants = await _unitOfWork.Consultants.FindAsync(c => c.BranchId == branchId && c.IsActive);
        var consultantList = consultants.ToList();
        var totalConsultants = consultantList.Count;

        // Generate time slots based on operating hours
        var slots = new List<AvailableSlotDto>();
        var slotDuration = TimeSpan.FromMinutes(service.DurationMinutes);
        var currentTime = hours.OpenTime;

        // Create 30-minute interval slots
        while (currentTime.Add(slotDuration) <= hours.CloseTime)
        {
            var endTime = currentTime.Add(slotDuration);
            var availableCount = await GetAvailableConsultantCountAsync(branchId, date, currentTime, endTime, consultantList);
            var isAvailable = availableCount > 0 && IsSlotInFuture(date, currentTime);

            slots.Add(new AvailableSlotDto
            {
                Date = date,
                StartTime = currentTime,
                EndTime = endTime,
                IsAvailable = isAvailable,
                AvailableConsultantCount = isAvailable ? availableCount : 0,
                TotalConsultantCount = totalConsultants
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
    /// Check if a specific time slot is available (at least one consultant available, minimum lead time)
    /// </summary>
    public async Task<bool> IsSlotAvailableAsync(int branchId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        // Check minimum lead time (1 hour in advance)
        if (!IsSlotInFuture(date, startTime))
            return false;

        // Get consultants for this branch
        var consultants = await _unitOfWork.Consultants.FindAsync(c => c.BranchId == branchId && c.IsActive);
        var consultantList = consultants.ToList();

        // Check if at least one consultant is available
        var availableCount = await GetAvailableConsultantCountAsync(branchId, date, startTime, endTime, consultantList);
        return availableCount > 0;
    }

    /// <summary>
    /// Check if the slot is at least 1 hour in the future
    /// </summary>
    private static bool IsSlotInFuture(DateTime date, TimeSpan startTime)
    {
        var slotDateTime = date.Add(startTime);
        return slotDateTime > DateTime.Now.AddHours(1);
    }

    /// <summary>
    /// Count how many consultants are available for a given time slot
    /// </summary>
    private async Task<int> GetAvailableConsultantCountAsync(
        int branchId, 
        DateTime date, 
        TimeSpan startTime, 
        TimeSpan endTime,
        IList<Core.Entities.Consultant> consultants)
    {
        var availableCount = 0;

        foreach (var consultant in consultants)
        {
            // Check for overlapping appointments for this consultant
            var candidateAppointments = await _unitOfWork.Appointments.FindAsync(a =>
                a.ConsultantId == consultant.Id &&
                a.AppointmentDate == date &&
                a.Status != AppointmentStatus.Cancelled &&
                a.StartTime < endTime);

            // Filter in memory for full overlap check
            var overlappingAppointments = candidateAppointments.Where(a =>
            {
                var effectiveEndTime = a.EndTime != TimeSpan.Zero
                    ? a.EndTime
                    : a.StartTime.Add(TimeSpan.FromMinutes(15)); // Default 15-min fallback

                return startTime < effectiveEndTime;
            }).ToList();

            if (!overlappingAppointments.Any())
            {
                availableCount++;
            }
        }

        return availableCount;
    }
}
