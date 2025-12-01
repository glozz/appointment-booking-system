using AutoMapper;
using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Application.Services;

/// <summary>
/// Service for managing consultant-related operations
/// </summary>
public class ConsultantService : IConsultantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ConsultantService> _logger;
    
    // Default operating hours (configurable per branch in the future)
    private static readonly TimeSpan DefaultOpenTime = new TimeSpan(8, 0, 0);  // 08:00
    private static readonly TimeSpan DefaultCloseTime = new TimeSpan(17, 0, 0); // 17:00
    private const int DefaultSlotDurationMinutes = 15;

    public ConsultantService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ConsultantService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ConsultantDto?> GetConsultantByIdAsync(int id)
    {
        _logger.LogDebug("Retrieving consultant with ID: {ConsultantId}", id);

        var consultant = await _unitOfWork.Consultants.GetByIdAsync(id);
        
        if (consultant == null)
        {
            _logger.LogWarning("Consultant not found with ID: {ConsultantId}", id);
            return null;
        }

        return _mapper.Map<ConsultantDto>(consultant);
    }

    /// <inheritdoc />
    public async Task<ConsultantDto?> GetConsultantByUserIdAsync(int userId)
    {
        _logger.LogDebug("Retrieving consultant for user ID: {UserId}", userId);

        var consultants = await _unitOfWork.Consultants.FindAsync(c => c.UserId == userId);
        var consultant = consultants.FirstOrDefault();
        
        if (consultant == null)
        {
            _logger.LogInformation("No consultant found for user ID: {UserId}", userId);
            return null;
        }

        return _mapper.Map<ConsultantDto>(consultant);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConsultantDto>> GetAllConsultantsAsync()
    {
        _logger.LogDebug("Retrieving all consultants");

        var consultants = await _unitOfWork.Consultants.GetAllAsync();
        return _mapper.Map<IEnumerable<ConsultantDto>>(consultants);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConsultantDto>> GetConsultantsByBranchAsync(int branchId)
    {
        _logger.LogDebug("Retrieving consultants for branch: {BranchId}", branchId);

        var consultants = await _unitOfWork.Consultants.FindAsync(c => c.BranchId == branchId && c.IsActive);
        return _mapper.Map<IEnumerable<ConsultantDto>>(consultants);
    }

    /// <inheritdoc />
    public async Task<ConsultantScheduleDto?> GetConsultantScheduleAsync(int consultantId, DateTime date)
    {
        _logger.LogDebug("Retrieving schedule for consultant: {ConsultantId} on {Date}", consultantId, date);

        var consultant = await _unitOfWork.Consultants.GetByIdAsync(consultantId);
        if (consultant == null)
        {
            _logger.LogWarning("Consultant not found with ID: {ConsultantId}", consultantId);
            return null;
        }

        // Get operating hours for the branch
        var operatingHours = await _unitOfWork.BranchOperatingHours.FindAsync(
            h => h.BranchId == consultant.BranchId && h.DayOfWeek == date.DayOfWeek);
        var hours = operatingHours.FirstOrDefault();

        var openTime = hours?.IsClosed == false ? hours.OpenTime : DefaultOpenTime;
        var closeTime = hours?.IsClosed == false ? hours.CloseTime : DefaultCloseTime;
        var isClosed = hours?.IsClosed ?? false;

        var schedule = new ConsultantScheduleDto
        {
            ConsultantId = consultantId,
            ConsultantName = $"{consultant.FirstName} {consultant.LastName}",
            Date = date.Date,
            OpenTime = openTime,
            CloseTime = closeTime,
            TimeSlots = new List<TimeSlotDto>()
        };

        if (isClosed)
        {
            return schedule;
        }

        // Get appointments for this consultant on this date
        var appointments = await _unitOfWork.Appointments.GetAllWithIncludesAsync();
        var consultantAppointments = appointments
            .Where(a => a.ConsultantId == consultantId && 
                       a.AppointmentDate.Date == date.Date &&
                       a.Status != AppointmentStatus.Cancelled)
            .ToList();

        // Generate time slots
        var currentTime = openTime;
        while (currentTime < closeTime)
        {
            var slotEndTime = currentTime.Add(TimeSpan.FromMinutes(DefaultSlotDurationMinutes));
            
            // Check if this slot has an appointment
            var appointment = consultantAppointments.FirstOrDefault(a => 
                a.StartTime <= currentTime && a.EndTime > currentTime);

            var slot = new TimeSlotDto
            {
                StartTime = currentTime,
                EndTime = slotEndTime,
                IsBooked = appointment != null,
                Appointment = appointment != null ? new AppointmentSummaryDto
                {
                    Id = appointment.Id,
                    ConfirmationCode = appointment.ConfirmationCode,
                    CustomerName = $"{appointment.Customer?.FirstName} {appointment.Customer?.LastName}",
                    BranchName = appointment.Branch?.Name ?? "",
                    ServiceName = appointment.Service?.Name ?? "",
                    ConsultantName = $"{consultant.FirstName} {consultant.LastName}",
                    AppointmentDate = appointment.AppointmentDate,
                    StartTime = appointment.StartTime,
                    EndTime = appointment.EndTime,
                    Status = appointment.Status
                } : null
            };

            schedule.TimeSlots.Add(slot);
            currentTime = slotEndTime;
        }

        return schedule;
    }
}
