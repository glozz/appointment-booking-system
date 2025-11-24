using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Application.Services;

public class AppointmentTypeService : IAppointmentTypeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AppointmentTypeService> _logger;

    public AppointmentTypeService(IUnitOfWork unitOfWork, ILogger<AppointmentTypeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<AppointmentTypeDto>> GetAllAsync()
    {
        var types = await _unitOfWork.AppointmentTypes.FindAsync(t => t.IsActive);
        
        return types.Select(t => new AppointmentTypeDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            DurationMinutes = t.DurationMinutes,
            Color = t.Color,
            IsActive = t.IsActive
        });
    }

    public async Task<AppointmentTypeDto?> GetByIdAsync(int id)
    {
        var type = await _unitOfWork.AppointmentTypes.GetByIdAsync(id);
        if (type == null) return null;

        return new AppointmentTypeDto
        {
            Id = type.Id,
            Name = type.Name,
            Description = type.Description,
            DurationMinutes = type.DurationMinutes,
            Color = type.Color,
            IsActive = type.IsActive
        };
    }
}
