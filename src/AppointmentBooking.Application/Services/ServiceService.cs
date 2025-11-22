using AutoMapper;
using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Exceptions;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Application.Services;

/// <summary>
/// Service for managing service offerings (e.g., Account Opening, Loan Consultation)
/// </summary>
public class ServiceService : IServiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ServiceService> _logger;

    public ServiceService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ServiceService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Create a new service offering
    /// </summary>
    public async Task<ServiceDto> CreateServiceAsync(ServiceDto dto)
    {
        _logger.LogInformation("Creating new service: {ServiceName}", dto.Name);

        var service = _mapper.Map<Service>(dto);
        service.IsActive = true;

        await _unitOfWork.Services.AddAsync(service);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Service created successfully: {ServiceName} with ID {ServiceId}", service.Name, service.Id);
        return _mapper.Map<ServiceDto>(service);
    }

    /// <summary>
    /// Get service by ID
    /// </summary>
    public async Task<ServiceDto?> GetServiceByIdAsync(int id)
    {
        _logger.LogDebug("Retrieving service with ID: {ServiceId}", id);

        var service = await _unitOfWork.Services.GetByIdAsync(id);
        
        if (service == null)
        {
            _logger.LogWarning("Service not found with ID: {ServiceId}", id);
            return null;
        }

        return _mapper.Map<ServiceDto>(service);
    }

    /// <summary>
    /// Get all services
    /// </summary>
    public async Task<IEnumerable<ServiceDto>> GetAllServicesAsync()
    {
        _logger.LogDebug("Retrieving all services");

        var services = await _unitOfWork.Services.GetAllAsync();
        return _mapper.Map<IEnumerable<ServiceDto>>(services);
    }

    /// <summary>
    /// Get only active services
    /// </summary>
    public async Task<IEnumerable<ServiceDto>> GetActiveServicesAsync()
    {
        _logger.LogDebug("Retrieving active services only");

        var services = await _unitOfWork.Services.FindAsync(s => s.IsActive);
        return _mapper.Map<IEnumerable<ServiceDto>>(services);
    }

    /// <summary>
    /// Update existing service
    /// </summary>
    public async Task<ServiceDto> UpdateServiceAsync(ServiceDto dto)
    {
        _logger.LogInformation("Updating service with ID: {ServiceId}", dto.Id);

        var service = await _unitOfWork.Services.GetByIdAsync(dto.Id);
        if (service == null)
        {
            _logger.LogError("Service not found for update: {ServiceId}", dto.Id);
            throw new NotFoundException("Service", dto.Id);
        }

        // Map updated values to existing entity
        _mapper.Map(dto, service);
        
        await _unitOfWork.Services.UpdateAsync(service);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Service updated successfully: {ServiceId}", dto.Id);
        return _mapper.Map<ServiceDto>(service);
    }

    /// <summary>
    /// Delete service
    /// </summary>
    public async Task<bool> DeleteServiceAsync(int id)
    {
        _logger.LogInformation("Deleting service with ID: {ServiceId}", id);

        var service = await _unitOfWork.Services.GetByIdAsync(id);
        if (service == null)
        {
            _logger.LogError("Service not found for deletion: {ServiceId}", id);
            throw new NotFoundException("Service", id);
        }

        await _unitOfWork.Services.DeleteAsync(service);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Service deleted successfully: {ServiceId}", id);
        return true;
    }
}
