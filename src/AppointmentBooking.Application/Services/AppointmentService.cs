using AutoMapper;
using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Exceptions;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Application.Services;

/// <summary>
/// Main service for managing appointments - handles booking, cancellation, and full lifecycle
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AppointmentService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IAvailabilityService _availabilityService;

    // Default slot duration in minutes (configurable)
    private const int DefaultSlotDurationMinutes = 15;
    
    // Default operating hours (configurable per branch in the future)
    private static readonly TimeSpan DefaultOpenTime = new TimeSpan(8, 0, 0);  // 08:00
    private static readonly TimeSpan DefaultCloseTime = new TimeSpan(17, 0, 0); // 17:00

    public AppointmentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AppointmentService> logger,
        INotificationService notificationService,
        IAvailabilityService availabilityService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _notificationService = notificationService;
        _availabilityService = availabilityService;
    }

    /// <summary>
    /// Create a new appointment with conflict detection, operating hours validation,
    /// double-booking prevention, and consultant auto-assignment
    /// </summary>
    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto)
    {
        _logger.LogInformation("Creating appointment for customer: {Email} at branch: {BranchId}", 
            dto.Customer.Email, dto.BranchId);

        // Get service details to calculate end time
        var service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceId);
        if (service == null)
        {
            _logger.LogError("Service not found: {ServiceId}", dto.ServiceId);
            throw new NotFoundException("Service", dto.ServiceId);
        }

        var endTime = dto.StartTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

        // Validate 15-minute slot increment
        ValidateSlotIncrement(dto.StartTime);

        // Validate operating hours
        await ValidateOperatingHoursAsync(dto.BranchId, dto.AppointmentDate, dto.StartTime, endTime);

        // Double-booking prevention: check if slot already exists for this branch
        await ValidateNoDoubleBookingAsync(dto.BranchId, dto.AppointmentDate, dto.StartTime);

        // Check slot availability (conflict detection + lead time validation)
        var isAvailable = await _availabilityService.IsSlotAvailableAsync(
            dto.BranchId, dto.AppointmentDate, dto.StartTime, endTime);
        
        if (!isAvailable)
        {
            _logger.LogWarning("Time slot not available for branch {BranchId} on {Date} at {Time}", 
                dto.BranchId, dto.AppointmentDate, dto.StartTime);
            throw new ConflictException("The selected time slot is not available. Please choose another time.");
        }

        // Auto-assign an available consultant
        var consultantId = await FindAvailableConsultantAsync(dto.BranchId, dto.AppointmentDate, dto.StartTime, endTime);
        if (consultantId == null)
        {
            _logger.LogWarning("No available consultant for branch {BranchId} on {Date} at {Time}", 
                dto.BranchId, dto.AppointmentDate, dto.StartTime);
            throw new ConflictException("No consultants are available at the selected time. Please choose another time.");
        }

        // Begin transaction to ensure customer and appointment are created atomically
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Find or create customer
            var customers = await _unitOfWork.Customers.FindAsync(c => c.Email == dto.Customer.Email);
            var customer = customers.FirstOrDefault();
            
            if (customer == null)
            {
                _logger.LogInformation("Creating new customer: {Email}", dto.Customer.Email);
                customer = new Customer
                {
                    FirstName = dto.Customer.FirstName,
                    LastName = dto.Customer.LastName,
                    Email = dto.Customer.Email,
                    Phone = dto.Customer.Phone,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();
            }

            // Generate unique confirmation code with collision checking
            var confirmationCode = await GenerateUniqueConfirmationCodeAsync();

            // Create appointment with unique confirmation code
            var appointment = new Appointment
            {
                ConfirmationCode = confirmationCode,
                CustomerId = customer.Id,
                BranchId = dto.BranchId,
                ServiceId = dto.ServiceId,
                ConsultantId = consultantId,
                AppointmentDate = dto.AppointmentDate,
                StartTime = dto.StartTime,
                EndTime = endTime,
                Status = AppointmentStatus.Confirmed,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Appointments.AddAsync(appointment);
            
            // Commit transaction - both customer and appointment are saved together
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Appointment created successfully with confirmation code: {ConfirmationCode}, assigned to consultant: {ConsultantId}", 
                appointment.ConfirmationCode, consultantId);

            // Send confirmation notification
            try
            {
                await _notificationService.SendAppointmentConfirmationAsync(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send confirmation notification for appointment: {ConfirmationCode}", 
                    appointment.ConfirmationCode);
            }

            // Return the appointment with all navigation properties loaded
            return await GetAppointmentByConfirmationCodeAsync(appointment.ConfirmationCode) 
                ?? throw new InvalidOperationException("Failed to retrieve created appointment");
        }
        catch
        {
            // Rollback transaction if any error occurs
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <summary>
    /// Validates that the start time is on a 15-minute increment (00, 15, 30, 45)
    /// </summary>
    private void ValidateSlotIncrement(TimeSpan startTime)
    {
        if (startTime.Minutes % DefaultSlotDurationMinutes != 0 || startTime.Seconds != 0)
        {
            _logger.LogWarning("Invalid slot time: {StartTime}. Must be on 15-minute increments.", startTime);
            throw new ValidationException($"Appointment time must be on {DefaultSlotDurationMinutes}-minute increments (e.g., 09:00, 09:15, 09:30, 09:45).");
        }
    }

    /// <summary>
    /// Validates that the appointment falls within operating hours
    /// </summary>
    private async Task ValidateOperatingHoursAsync(int branchId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        // Get branch operating hours for the specific day
        var operatingHours = await _unitOfWork.BranchOperatingHours.FindAsync(
            h => h.BranchId == branchId && h.DayOfWeek == date.DayOfWeek);
        var hours = operatingHours.FirstOrDefault();

        // Use branch-specific hours if available, otherwise use defaults
        var openTime = hours?.IsClosed == false ? hours.OpenTime : DefaultOpenTime;
        var closeTime = hours?.IsClosed == false ? hours.CloseTime : DefaultCloseTime;

        // Check if branch is closed on this day
        if (hours?.IsClosed == true)
        {
            _logger.LogWarning("Branch {BranchId} is closed on {DayOfWeek}", branchId, date.DayOfWeek);
            throw new ValidationException($"The branch is closed on {date.DayOfWeek}. Please select a different day.");
        }

        // Validate against operating hours
        if (startTime < openTime || endTime > closeTime)
        {
            _logger.LogWarning("Appointment time {StartTime}-{EndTime} outside operating hours {OpenTime}-{CloseTime}", 
                startTime, endTime, openTime, closeTime);
            throw new ValidationException($"Appointments are only available during operating hours ({openTime:hh\\:mm} - {closeTime:hh\\:mm}).");
        }
    }

    /// <summary>
    /// Checks if a booking already exists for the same branch, date, and start time
    /// </summary>
    private async Task ValidateNoDoubleBookingAsync(int branchId, DateTime date, TimeSpan startTime)
    {
        var existingAppointments = await _unitOfWork.Appointments.FindAsync(a =>
            a.BranchId == branchId &&
            a.AppointmentDate == date &&
            a.StartTime == startTime &&
            a.Status != AppointmentStatus.Cancelled);

        if (existingAppointments.Any())
        {
            _logger.LogWarning("Double booking detected for branch {BranchId} on {Date} at {Time}", 
                branchId, date, startTime);
            throw new ConflictException("This time slot is already booked. Please choose another time.");
        }
    }

    /// <summary>
    /// Finds an available consultant for the given time slot using robust overlap detection.
    /// A consultant is available if they have no overlapping appointments:
    /// existing.Start < requestedEnd AND requestedStart < (existing.End or existing.Start + duration)
    /// </summary>
    private async Task<int?> FindAvailableConsultantAsync(int branchId, DateTime date, TimeSpan requestedStart, TimeSpan requestedEnd)
    {
        // Get all active consultants for this branch
        var consultants = await _unitOfWork.Consultants.FindAsync(c => c.BranchId == branchId && c.IsActive);
        
        if (!consultants.Any())
        {
            _logger.LogWarning("No consultants found for branch {BranchId}", branchId);
            return null;
        }

        foreach (var consultant in consultants)
        {
            // Check for overlapping appointments for this consultant
            // Note: The fallback to DefaultSlotDurationMinutes is only for legacy/malformed data
            // where EndTime might not be set. All properly created appointments have EndTime set.
            var candidateAppointments = await _unitOfWork.Appointments.FindAsync(a =>
              a.ConsultantId == consultant.Id &&
              a.AppointmentDate == date &&
              a.Status != AppointmentStatus.Cancelled &&
              a.StartTime < requestedEnd);

            // Filter in memory with the complex logic
            var overlappingAppointments = candidateAppointments.Where(a =>
            {
                var effectiveEndTime = a.EndTime != TimeSpan.Zero
                    ? a.EndTime
                    : a.StartTime.Add(TimeSpan.FromMinutes(DefaultSlotDurationMinutes));

                return requestedStart < effectiveEndTime;
            }).ToList();

            if (!overlappingAppointments.Any())
            {
                _logger.LogDebug("Found available consultant {ConsultantId} for branch {BranchId}", consultant.Id, branchId);
                return consultant.Id;
            }
        }

        _logger.LogDebug("No available consultants found for branch {BranchId} at {Date} {StartTime}-{EndTime}", 
            branchId, date, requestedStart, requestedEnd);
        return null;
    }

    /// <summary>
    /// Get appointment by ID with navigation properties loaded using optimized eager loading
    /// </summary>
    public async Task<AppointmentDto?> GetAppointmentByIdAsync(int id)
    {
        _logger.LogDebug("Retrieving appointment with ID: {AppointmentId}", id);

        // Use specialized repository method with eager loading
        var appointment = await _unitOfWork.Appointments.GetByIdWithIncludesAsync(id);
        
        if (appointment == null)
        {
            _logger.LogWarning("Appointment not found with ID: {AppointmentId}", id);
            return null;
        }

        // Map directly - navigation properties are already loaded
        return _mapper.Map<AppointmentDto>(appointment);
    }

    /// <summary>
    /// Get appointment by unique confirmation code with all navigation properties loaded using optimized eager loading
    /// </summary>
    public async Task<AppointmentDto?> GetAppointmentByConfirmationCodeAsync(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
        {
            _logger.LogWarning("Empty confirmation code provided.");
            return null;
        }

        _logger.LogDebug("Retrieving appointment with confirmation code: {Code}", confirmationCode);

        // Use specialized repository method with eager loading
        var appointment = await _unitOfWork.Appointments.GetByConfirmationCodeWithIncludesAsync(confirmationCode);

        if (appointment == null)
        {
            _logger.LogInformation("No appointment found for confirmation code: {Code}", confirmationCode);
            return null;
        }

        // Map directly - navigation properties are already loaded
        return _mapper.Map<AppointmentDto>(appointment);
    }

    /// <summary>
    /// Get all appointments with navigation properties using optimized eager loading.
    /// Uses a single database query instead of N+1 queries.
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync()
    {
        _logger.LogDebug("Retrieving all appointments");

        // Use specialized repository method with eager loading
        var appointments = await _unitOfWork.Appointments.GetAllWithIncludesAsync();
        
        // Map all appointments - navigation properties are already loaded
        return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
    }

    /// <summary>
    /// Get all appointments for a specific customer by email using optimized eager loading
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByCustomerEmailAsync(string email)
    {
        _logger.LogDebug("Retrieving appointments for customer: {Email}", email);

        var customers = await _unitOfWork.Customers.FindAsync(c => c.Email == email);
        var customer = customers.FirstOrDefault();
        
        if (customer == null)
        {
            _logger.LogInformation("No customer found with email: {Email}", email);
            return Enumerable.Empty<AppointmentDto>();
        }

        // Use specialized repository method with eager loading
        var appointments = await _unitOfWork.Appointments.GetByCustomerIdWithIncludesAsync(customer.Id);
        
        // Map all appointments - navigation properties are already loaded
        return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
    }

    /// <summary>
    /// Get all appointments for a specific branch using optimized eager loading
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByBranchAsync(int branchId)
    {
        _logger.LogDebug("Retrieving appointments for branch: {BranchId}", branchId);

        // Use specialized repository method with eager loading
        var appointments = await _unitOfWork.Appointments.GetByBranchIdWithIncludesAsync(branchId);
        
        // Map all appointments - navigation properties are already loaded
        return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
    }

    /// <summary>
    /// Update an existing appointment
    /// </summary>
    public async Task<AppointmentDto> UpdateAppointmentAsync(UpdateAppointmentDto dto)
    {
        _logger.LogInformation("Updating appointment: {AppointmentId}", dto.Id);

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(dto.Id);
        if (appointment == null)
        {
            _logger.LogError("Appointment not found for update: {AppointmentId}", dto.Id);
            throw new NotFoundException("Appointment", dto.Id);
        }

        appointment.AppointmentDate = dto.AppointmentDate;
        appointment.StartTime = dto.StartTime;
        appointment.Status = dto.Status;
        appointment.Notes = dto.Notes;
        appointment.CancellationReason = dto.CancellationReason;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Appointments.UpdateAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Appointment updated successfully: {AppointmentId}", dto.Id);
        
        // Reload appointment with navigation properties for return
        var updatedAppointment = await _unitOfWork.Appointments.GetByIdWithIncludesAsync(dto.Id);
        return _mapper.Map<AppointmentDto>(updatedAppointment!);
    }

    /// <summary>
    /// Cancel an appointment with reason
    /// </summary>
    public async Task<bool> CancelAppointmentAsync(int id, string cancellationReason)
    {
        _logger.LogInformation("Cancelling appointment: {AppointmentId}", id);

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment == null)
        {
            _logger.LogError("Appointment not found for cancellation: {AppointmentId}", id);
            throw new NotFoundException("Appointment", id);
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = cancellationReason;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Appointments.UpdateAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Appointment cancelled successfully: {AppointmentId}", id);

        // Send cancellation notification
        try
        {
            await _notificationService.SendCancellationNotificationAsync(appointment);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send cancellation notification for appointment: {AppointmentId}", id);
        }

        return true;
    }

    /// <summary>
    /// Update appointment status only
    /// </summary>
    public async Task<bool> UpdateAppointmentStatusAsync(int id, AppointmentStatus status)
    {
        _logger.LogInformation("Updating appointment status: {AppointmentId} to {Status}", id, status);

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment == null)
        {
            _logger.LogError("Appointment not found for status update: {AppointmentId}", id);
            throw new NotFoundException("Appointment", id);
        }

        appointment.Status = status;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Appointments.UpdateAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Appointment status updated successfully: {AppointmentId}", id);
        return true;
    }

    /// <summary>
    /// Generates a unique confirmation code with collision checking.
    /// Format: APT-YYYYMMDD-XXXXX (e.g., APT-20251122-A7B9K)
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if unable to generate a unique code after max attempts.</exception>
    private async Task<string> GenerateUniqueConfirmationCodeAsync()
    {
        const int maxAttempts = 10;
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var randomPart = new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            
            var code = $"APT-{DateTime.UtcNow:yyyyMMdd}-{randomPart}";
            
            // Check if code already exists in database
            var exists = await _unitOfWork.Appointments.ConfirmationCodeExistsAsync(code);
            
            if (!exists)
            {
                _logger.LogDebug("Generated unique confirmation code: {ConfirmationCode} on attempt {Attempt}", code, attempt);
                return code;
            }
            
            _logger.LogDebug("Confirmation code collision detected for: {ConfirmationCode}, regenerating (attempt {Attempt}/{MaxAttempts})", 
                code, attempt, maxAttempts);
        }
        
        _logger.LogError("Failed to generate unique confirmation code after {MaxAttempts} attempts", maxAttempts);
        throw new InvalidOperationException($"Unable to generate unique confirmation code after {maxAttempts} attempts. Please try again.");
    }
}
