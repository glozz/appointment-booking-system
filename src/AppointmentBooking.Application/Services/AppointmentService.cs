using System.Security.Cryptography;
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
    private readonly IAvailabilityService _availabilityService;

    // Default slot duration in minutes (configurable)
    private const int DefaultSlotDurationMinutes = 15;
    
    // Default operating hours (configurable per branch in the future)
    private static readonly TimeSpan DefaultOpenTime = new TimeSpan(8, 0, 0);  // 08:00
    private static readonly TimeSpan DefaultCloseTime = new TimeSpan(17, 0, 0); // 17:00

    // Note: Notification features have been permanently removed as per requirements.
    // The INotificationService dependency is no longer needed.
    public AppointmentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AppointmentService> logger,
        IAvailabilityService availabilityService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
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

        // Customer cross-branch conflict detection: prevent same customer from booking at overlapping times
        await ValidateCustomerAvailabilityAsync(dto.Customer.Email, dto.BranchId, dto.AppointmentDate, dto.StartTime, endTime);

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

            // Return the appointment with all navigation properties loaded
            return await GetAppointmentByConfirmationCodeAsync(appointment.ConfirmationCode) 
                ?? throw new InvalidOperationException("Failed to retrieve created appointment");
        }
        catch
        {
            // Only rollback if a transaction was actually started
            if (_unitOfWork.HasActiveTransaction())
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
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
    /// Validates that the customer does not have an overlapping appointment at any branch.
    /// A customer cannot be in two places at the same time.
    /// </summary>
    private async Task ValidateCustomerAvailabilityAsync(string customerEmail, int requestedBranchId, DateTime date, TimeSpan requestedStart, TimeSpan requestedEnd)
    {
        // Get customer by email
        var customers = await _unitOfWork.Customers.FindAsync(c => c.Email == customerEmail);
        var customer = customers.FirstOrDefault();
        
        if (customer == null)
        {
            // New customer, no conflicts possible
            return;
        }

        // Find appointments where customer has overlapping times at any branch
        var candidateAppointments = await _unitOfWork.Appointments.FindAsync(a =>
            a.CustomerId == customer.Id &&
            a.AppointmentDate == date &&
            a.Status != AppointmentStatus.Cancelled &&
            a.StartTime < requestedEnd);

        // Filter in memory with overlap logic
        var overlappingAppointments = candidateAppointments.Where(a =>
        {
            var effectiveEndTime = a.EndTime != TimeSpan.Zero
                ? a.EndTime
                : a.StartTime.Add(TimeSpan.FromMinutes(DefaultSlotDurationMinutes));

            return requestedStart < effectiveEndTime;
        }).ToList();

        if (overlappingAppointments.Any())
        {
            var existingAppointment = overlappingAppointments.First();
            
            // Load branch info for the error message
            var branch = await _unitOfWork.Branches.GetByIdAsync(existingAppointment.BranchId);
            var branchName = branch?.Name ?? "another branch";
            
            _logger.LogWarning("Customer {Email} already has appointment at {BranchName} on {Date} at {Time}",
                customerEmail, branchName, date, existingAppointment.StartTime);
            
            throw new ConflictException($"You already have an appointment at {branchName} on {date:MMMM d, yyyy} at {existingAppointment.StartTime:hh\\:mm}. Please choose a different time or cancel your existing appointment.");
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

    #region Consultant Appointment Views

    /// <summary>
    /// Get upcoming appointments for a consultant (today and future, ordered by date/time ASC)
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetConsultantUpcomingAppointmentsAsync(int consultantId)
    {
        _logger.LogDebug("Retrieving upcoming appointments for consultant: {ConsultantId}", consultantId);

        var today = DateTime.UtcNow.Date;
        var appointments = await _unitOfWork.Appointments.GetAllWithIncludesAsync();
        
        var upcoming = appointments
            .Where(a => a.ConsultantId == consultantId && 
                       a.AppointmentDate >= today &&
                       a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToList();

        return _mapper.Map<IEnumerable<AppointmentDto>>(upcoming);
    }

    /// <summary>
    /// Get past appointments for a consultant (before today or completed, ordered by date/time DESC)
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetConsultantPastAppointmentsAsync(int consultantId)
    {
        _logger.LogDebug("Retrieving past appointments for consultant: {ConsultantId}", consultantId);

        var today = DateTime.UtcNow.Date;
        var appointments = await _unitOfWork.Appointments.GetAllWithIncludesAsync();
        
        var past = appointments
            .Where(a => a.ConsultantId == consultantId && 
                       (a.AppointmentDate < today || a.Status == AppointmentStatus.Completed))
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ToList();

        return _mapper.Map<IEnumerable<AppointmentDto>>(past);
    }

    /// <summary>
    /// Get today's appointments for a consultant (ordered by start time ASC)
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetConsultantTodayAppointmentsAsync(int consultantId)
    {
        _logger.LogDebug("Retrieving today's appointments for consultant: {ConsultantId}", consultantId);

        var today = DateTime.UtcNow.Date;
        var appointments = await _unitOfWork.Appointments.GetAllWithIncludesAsync();
        
        var todayAppointments = appointments
            .Where(a => a.ConsultantId == consultantId && 
                       a.AppointmentDate == today &&
                       a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.StartTime)
            .ToList();

        return _mapper.Map<IEnumerable<AppointmentDto>>(todayAppointments);
    }

    /// <summary>
    /// Get appointments for a consultant within a date range
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetConsultantAppointmentsByDateRangeAsync(int consultantId, DateTime startDate, DateTime endDate)
    {
        _logger.LogDebug("Retrieving appointments for consultant: {ConsultantId} from {StartDate} to {EndDate}", 
            consultantId, startDate, endDate);

        var appointments = await _unitOfWork.Appointments.GetAllWithIncludesAsync();
        
        var rangeAppointments = appointments
            .Where(a => a.ConsultantId == consultantId && 
                       a.AppointmentDate >= startDate.Date &&
                       a.AppointmentDate <= endDate.Date)
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToList();

        return _mapper.Map<IEnumerable<AppointmentDto>>(rangeAppointments);
    }

    #endregion

    #region Customer Appointment Views

    /// <summary>
    /// Get upcoming appointments for a customer (today and future, ordered by date/time ASC)
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetCustomerUpcomingAppointmentsAsync(string email)
    {
        _logger.LogDebug("Retrieving upcoming appointments for customer: {Email}", email);

        var customers = await _unitOfWork.Customers.FindAsync(c => c.Email == email);
        var customer = customers.FirstOrDefault();
        
        if (customer == null)
        {
            return Enumerable.Empty<AppointmentDto>();
        }

        var today = DateTime.UtcNow.Date;
        var appointments = await _unitOfWork.Appointments.GetByCustomerIdWithIncludesAsync(customer.Id);
        
        var upcoming = appointments
            .Where(a => a.AppointmentDate >= today &&
                       a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToList();

        return _mapper.Map<IEnumerable<AppointmentDto>>(upcoming);
    }

    /// <summary>
    /// Get past appointments for a customer (before today or completed, ordered by date/time DESC)
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetCustomerPastAppointmentsAsync(string email)
    {
        _logger.LogDebug("Retrieving past appointments for customer: {Email}", email);

        var customers = await _unitOfWork.Customers.FindAsync(c => c.Email == email);
        var customer = customers.FirstOrDefault();
        
        if (customer == null)
        {
            return Enumerable.Empty<AppointmentDto>();
        }

        var today = DateTime.UtcNow.Date;
        var appointments = await _unitOfWork.Appointments.GetByCustomerIdWithIncludesAsync(customer.Id);
        
        var past = appointments
            .Where(a => a.AppointmentDate < today || a.Status == AppointmentStatus.Completed)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ToList();

        return _mapper.Map<IEnumerable<AppointmentDto>>(past);
    }

    /// <summary>
    /// Get customer appointments filtered by status
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetCustomerAppointmentsByStatusAsync(string email, AppointmentStatus status)
    {
        _logger.LogDebug("Retrieving appointments for customer: {Email} with status: {Status}", email, status);

        var customers = await _unitOfWork.Customers.FindAsync(c => c.Email == email);
        var customer = customers.FirstOrDefault();
        
        if (customer == null)
        {
            return Enumerable.Empty<AppointmentDto>();
        }

        var appointments = await _unitOfWork.Appointments.GetByCustomerIdWithIncludesAsync(customer.Id);
        
        var filtered = appointments
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ToList();

        return _mapper.Map<IEnumerable<AppointmentDto>>(filtered);
    }

    /// <summary>
    /// Get customer appointments within a date range
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetCustomerAppointmentsByDateRangeAsync(string email, DateTime startDate, DateTime endDate)
    {
        _logger.LogDebug("Retrieving appointments for customer: {Email} from {StartDate} to {EndDate}", 
            email, startDate, endDate);

        var customers = await _unitOfWork.Customers.FindAsync(c => c.Email == email);
        var customer = customers.FirstOrDefault();
        
        if (customer == null)
        {
            return Enumerable.Empty<AppointmentDto>();
        }

        var appointments = await _unitOfWork.Appointments.GetByCustomerIdWithIncludesAsync(customer.Id);
        
        var rangeAppointments = appointments
            .Where(a => a.AppointmentDate >= startDate.Date &&
                       a.AppointmentDate <= endDate.Date)
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToList();

        return _mapper.Map<IEnumerable<AppointmentDto>>(rangeAppointments);
    }

    #endregion

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
    /// Uses cryptographically secure random generation for better uniqueness.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if unable to generate a unique code after max attempts.</exception>
    private async Task<string> GenerateUniqueConfirmationCodeAsync()
    {
        const int maxAttempts = 10;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            // Use cryptographically secure random for better uniqueness
            var randomBytes = RandomNumberGenerator.GetBytes(5);
            var randomPart = new string(randomBytes.Select(b => chars[b % chars.Length]).ToArray());
            
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
