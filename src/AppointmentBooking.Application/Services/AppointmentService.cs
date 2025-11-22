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
    /// Create a new appointment with conflict detection and confirmation code generation
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

        // Check slot availability (conflict detection + lead time validation)
        var isAvailable = await _availabilityService.IsSlotAvailableAsync(
            dto.BranchId, dto.AppointmentDate, dto.StartTime, endTime);
        
        if (!isAvailable)
        {
            _logger.LogWarning("Time slot not available for branch {BranchId} on {Date} at {Time}", 
                dto.BranchId, dto.AppointmentDate, dto.StartTime);
            throw new ConflictException("The selected time slot is not available. Please choose another time.");
        }

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

        // Create appointment with unique confirmation code
        var appointment = new Appointment
        {
            ConfirmationCode = GenerateConfirmationCode(),
            CustomerId = customer.Id,
            BranchId = dto.BranchId,
            ServiceId = dto.ServiceId,
            AppointmentDate = dto.AppointmentDate,
            StartTime = dto.StartTime,
            EndTime = endTime,
            Status = AppointmentStatus.Confirmed,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Appointments.AddAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Appointment created successfully with confirmation code: {ConfirmationCode}", 
            appointment.ConfirmationCode);

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

        var createdAppointment = await _unitOfWork.Appointments.GetByIdAsync(appointment.Id);
        return _mapper.Map<AppointmentDto>(createdAppointment);
    }

    /// <summary>
    /// Get appointment by ID
    /// </summary>
    public async Task<AppointmentDto?> GetAppointmentByIdAsync(int id)
    {
        _logger.LogDebug("Retrieving appointment with ID: {AppointmentId}", id);

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        
        if (appointment == null)
        {
            _logger.LogWarning("Appointment not found with ID: {AppointmentId}", id);
            return null;
        }

        return _mapper.Map<AppointmentDto>(appointment);
    }

    /// <summary>
    /// Get appointment by unique confirmation code
    /// </summary>
    public async Task<AppointmentDto?> GetAppointmentByConfirmationCodeAsync(string confirmationCode)
    {
        _logger.LogDebug("Retrieving appointment with confirmation code: {ConfirmationCode}", confirmationCode);

        var appointments = await _unitOfWork.Appointments.FindAsync(a => 
            a.ConfirmationCode == confirmationCode);
        var appointment = appointments.FirstOrDefault();
        
        if (appointment == null)
        {
            _logger.LogWarning("Appointment not found with confirmation code: {ConfirmationCode}", confirmationCode);
            return null;
        }

        return _mapper.Map<AppointmentDto>(appointment);
    }

    /// <summary>
    /// Get all appointments
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync()
    {
        _logger.LogDebug("Retrieving all appointments");

        var appointments = await _unitOfWork.Appointments.GetAllAsync();
        return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
    }

    /// <summary>
    /// Get all appointments for a specific customer by email
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

        var appointments = await _unitOfWork.Appointments.FindAsync(a => a.CustomerId == customer.Id);
        return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
    }

    /// <summary>
    /// Get all appointments for a specific branch
    /// </summary>
    public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByBranchAsync(int branchId)
    {
        _logger.LogDebug("Retrieving appointments for branch: {BranchId}", branchId);

        var appointments = await _unitOfWork.Appointments.FindAsync(a => a.BranchId == branchId);
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
        return _mapper.Map<AppointmentDto>(appointment);
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
    /// Generate unique confirmation code in format: APT-YYYYMMDD-XXXXX
    /// Example: APT-20251122-A7B9K
    /// </summary>
    private string GenerateConfirmationCode()
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var randomPart = new string(Enumerable.Repeat(chars, 5)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        
        var code = $"APT-{DateTime.UtcNow:yyyyMMdd}-{randomPart}";
        
        _logger.LogDebug("Generated confirmation code: {ConfirmationCode}", code);
        return code;
    }
}
