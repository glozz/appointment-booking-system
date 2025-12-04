using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Entities;
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
    private const int BcryptWorkFactor = 12;

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

        var consultant = await _unitOfWork.Consultants.GetByIdWithIncludesAsync(id, c => c.Branch);
        
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

        var consultant = await _unitOfWork.Consultants.Query()
            .Include(c => c.Branch)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        
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

        var consultants = await _unitOfWork.Consultants.Query()
            .Include(c => c.Branch)
            .ToListAsync();
        return _mapper.Map<IEnumerable<ConsultantDto>>(consultants);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConsultantDto>> GetConsultantsByBranchAsync(int branchId)
    {
        _logger.LogDebug("Retrieving consultants for branch: {BranchId}", branchId);

        var consultants = await _unitOfWork.Consultants.Query()
            .Include(c => c.Branch)
            .Where(c => c.BranchId == branchId && c.IsActive)
            .ToListAsync();
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

    /// <inheritdoc />
    public async Task<ConsultantRegistrationResultDto> RegisterConsultantAsync(ConsultantRegistrationDto dto)
    {
        _logger.LogInformation("Consultant registration attempt for email: {Email}", dto.Email);

        // Check if email already exists
        var existingUsers = await _unitOfWork.Users.FindAsync(u => u.Email == dto.Email.ToLowerInvariant());
        if (existingUsers.Any())
        {
            _logger.LogWarning("Consultant registration failed - email already exists: {Email}", dto.Email);
            return new ConsultantRegistrationResultDto 
            { 
                Success = false, 
                Message = "Email address is already registered." 
            };
        }

        // Validate password length (min 6 chars as per requirements)
        if (string.IsNullOrEmpty(dto.Password) || dto.Password.Length < 6)
        {
            return new ConsultantRegistrationResultDto 
            { 
                Success = false, 
                Message = "Password must be at least 6 characters." 
            };
        }

        // Verify branch exists
        var branch = await _unitOfWork.Branches.GetByIdAsync(dto.BranchId);
        if (branch == null)
        {
            _logger.LogWarning("Consultant registration failed - invalid branch: {BranchId}", dto.BranchId);
            return new ConsultantRegistrationResultDto 
            { 
                Success = false, 
                Message = "Selected branch does not exist." 
            };
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Create inactive user account
            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BcryptWorkFactor),
                Phone = dto.Phone,
                Role = UserRole.Consultant,
                IsActive = false, // Inactive until admin approves
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Create consultant profile (also inactive)
            var consultant = new Consultant
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                BranchId = dto.BranchId,
                UserId = user.Id,
                IsActive = false, // Inactive until admin approves
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Consultants.AddAsync(consultant);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Consultant registered successfully (pending approval): {Email}, ConsultantId: {ConsultantId}", 
                dto.Email, consultant.Id);

            return new ConsultantRegistrationResultDto
            {
                Success = true,
                Message = "Registration successful. Your account is pending admin approval.",
                ConsultantId = consultant.Id
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error during consultant registration for {Email}", dto.Email);
            return new ConsultantRegistrationResultDto 
            { 
                Success = false, 
                Message = "An error occurred during registration. Please try again." 
            };
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PendingConsultantDto>> GetPendingConsultantsAsync()
    {
        _logger.LogDebug("Retrieving pending consultants");

        var pendingConsultants = await _unitOfWork.Consultants.Query()
            .Include(c => c.Branch)
            .Include(c => c.User)
            .Where(c => !c.IsActive && c.UserId != null && c.User != null && !c.User.IsActive)
            .ToListAsync();

        return pendingConsultants.Select(c => new PendingConsultantDto
        {
            ConsultantId = c.Id,
            UserId = c.UserId!.Value,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.User!.Email,
            Phone = c.User.Phone,
            BranchId = c.BranchId,
            BranchName = c.Branch?.Name ?? "",
            RegisteredAt = c.CreatedAt
        });
    }

    /// <inheritdoc />
    public async Task<bool> ActivateConsultantAsync(int consultantId)
    {
        _logger.LogInformation("Activating consultant with ID: {ConsultantId}", consultantId);

        var consultant = await _unitOfWork.Consultants.GetByIdWithIncludesAsync(consultantId, c => c.User!);

        if (consultant == null)
        {
            _logger.LogWarning("Consultant not found for activation: {ConsultantId}", consultantId);
            return false;
        }

        if (consultant.User == null)
        {
            _logger.LogWarning("Consultant has no associated user: {ConsultantId}", consultantId);
            return false;
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Activate user account
            consultant.User.IsActive = true;
            consultant.User.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(consultant.User);

            // Activate consultant profile
            consultant.IsActive = true;
            await _unitOfWork.Consultants.UpdateAsync(consultant);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Consultant activated successfully: {ConsultantId}", consultantId);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error activating consultant: {ConsultantId}", consultantId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RejectConsultantAsync(int consultantId)
    {
        _logger.LogInformation("Rejecting consultant registration: {ConsultantId}", consultantId);

        var consultant = await _unitOfWork.Consultants.GetByIdWithIncludesAsync(consultantId, c => c.User!);

        if (consultant == null)
        {
            _logger.LogWarning("Consultant not found for rejection: {ConsultantId}", consultantId);
            return false;
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Delete consultant profile first
            await _unitOfWork.Consultants.DeleteAsync(consultant);

            // Delete associated user if exists
            if (consultant.User != null)
            {
                await _unitOfWork.Users.DeleteAsync(consultant.User);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Consultant registration rejected and deleted: {ConsultantId}", consultantId);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error rejecting consultant: {ConsultantId}", consultantId);
            return false;
        }
    }
}
