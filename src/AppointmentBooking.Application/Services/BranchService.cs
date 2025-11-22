using AutoMapper;
using Microsoft.Extensions.Logging;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Exceptions;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Application.Services;

/// <summary>
/// Service for managing branch operations
/// </summary>
public class BranchService : IBranchService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<BranchService> _logger;

    public BranchService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<BranchService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Create a new branch
    /// </summary>
    public async Task<BranchDto> CreateBranchAsync(CreateBranchDto dto)
    {
        _logger.LogInformation("Creating new branch: {BranchName}", dto.Name);

        var branch = _mapper.Map<Branch>(dto);
        branch.IsActive = true;

        await _unitOfWork.Branches.AddAsync(branch);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Branch created successfully: {BranchName} with ID {BranchId}", branch.Name, branch.Id);
        return _mapper.Map<BranchDto>(branch);
    }

    /// <summary>
    /// Get branch by ID
    /// </summary>
    public async Task<BranchDto?> GetBranchByIdAsync(int id)
    {
        _logger.LogDebug("Retrieving branch with ID: {BranchId}", id);

        var branch = await _unitOfWork.Branches.GetByIdAsync(id);
        
        if (branch == null)
        {
            _logger.LogWarning("Branch not found with ID: {BranchId}", id);
            return null;
        }

        return _mapper.Map<BranchDto>(branch);
    }

    /// <summary>
    /// Get all branches
    /// </summary>
    public async Task<IEnumerable<BranchDto>> GetAllBranchesAsync()
    {
        _logger.LogDebug("Retrieving all branches");

        var branches = await _unitOfWork.Branches.GetAllAsync();
        return _mapper.Map<IEnumerable<BranchDto>>(branches);
    }

    /// <summary>
    /// Get only active branches
    /// </summary>
    public async Task<IEnumerable<BranchDto>> GetActiveBranchesAsync()
    {
        _logger.LogDebug("Retrieving active branches only");

        var branches = await _unitOfWork.Branches.FindAsync(b => b.IsActive);
        return _mapper.Map<IEnumerable<BranchDto>>(branches);
    }

    /// <summary>
    /// Update existing branch
    /// </summary>
    public async Task<BranchDto> UpdateBranchAsync(BranchDto dto)
    {
        _logger.LogInformation("Updating branch with ID: {BranchId}", dto.Id);

        var branch = await _unitOfWork.Branches.GetByIdAsync(dto.Id);
        if (branch == null)
        {
            _logger.LogError("Branch not found for update: {BranchId}", dto.Id);
            throw new NotFoundException("Branch", dto.Id);
        }

        // Map updated values to existing entity
        _mapper.Map(dto, branch);
        
        await _unitOfWork.Branches.UpdateAsync(branch);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Branch updated successfully: {BranchId}", dto.Id);
        return _mapper.Map<BranchDto>(branch);
    }

    /// <summary>
    /// Delete branch
    /// </summary>
    public async Task<bool> DeleteBranchAsync(int id)
    {
        _logger.LogInformation("Deleting branch with ID: {BranchId}", id);

        var branch = await _unitOfWork.Branches.GetByIdAsync(id);
        if (branch == null)
        {
            _logger.LogError("Branch not found for deletion: {BranchId}", id);
            throw new NotFoundException("Branch", id);
        }

        await _unitOfWork.Branches.DeleteAsync(branch);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Branch deleted successfully: {BranchId}", id);
        return true;
    }
}
