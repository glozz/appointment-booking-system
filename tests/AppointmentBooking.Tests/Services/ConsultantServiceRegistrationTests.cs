using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Services;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Tests.Services;

public class ConsultantServiceRegistrationTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<ConsultantService>> _loggerMock;
    private readonly ConsultantService _consultantService;

    public ConsultantServiceRegistrationTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<ConsultantService>>();

        _consultantService = new ConsultantService(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterConsultantAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var dto = new ConsultantRegistrationDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.consultant@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            BranchId = 1,
            Phone = "555-1234"
        };

        var branch = new Branch { Id = 1, Name = "Main Branch", IsActive = true };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<User>());
        userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = 1; return u; });

        var branchRepoMock = new Mock<IRepository<Branch>>();
        branchRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(branch);

        var consultantRepoMock = new Mock<IRepository<Consultant>>();
        consultantRepoMock.Setup(r => r.AddAsync(It.IsAny<Consultant>()))
            .ReturnsAsync((Consultant c) => { c.Id = 1; return c; });

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Branches).Returns(branchRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Consultants).Returns(consultantRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _consultantService.RegisterConsultantAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ConsultantId);
        Assert.Contains("pending", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterConsultantAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var dto = new ConsultantRegistrationDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            BranchId = 1
        };

        var existingUser = new User { Email = "existing@example.com" };
        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new[] { existingUser });

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        // Act
        var result = await _consultantService.RegisterConsultantAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already registered", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterConsultantAsync_WithShortPassword_ReturnsFailure()
    {
        // Arrange
        var dto = new ConsultantRegistrationDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "12345",  // Too short
            ConfirmPassword = "12345",
            BranchId = 1
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<User>());

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        // Act
        var result = await _consultantService.RegisterConsultantAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("6 characters", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterConsultantAsync_WithInvalidBranch_ReturnsFailure()
    {
        // Arrange
        var dto = new ConsultantRegistrationDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            BranchId = 999  // Non-existent branch
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<User>());

        var branchRepoMock = new Mock<IRepository<Branch>>();
        branchRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Branch?)null);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Branches).Returns(branchRepoMock.Object);

        // Act
        var result = await _consultantService.RegisterConsultantAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("branch", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterConsultantAsync_CreatesInactiveUserAndConsultant()
    {
        // Arrange
        var dto = new ConsultantRegistrationDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            BranchId = 1
        };

        var branch = new Branch { Id = 1, Name = "Main Branch", IsActive = true };
        User? createdUser = null;
        Consultant? createdConsultant = null;

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<User>());
        userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => createdUser = u)
            .ReturnsAsync((User u) => { u.Id = 1; return u; });

        var branchRepoMock = new Mock<IRepository<Branch>>();
        branchRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(branch);

        var consultantRepoMock = new Mock<IRepository<Consultant>>();
        consultantRepoMock.Setup(r => r.AddAsync(It.IsAny<Consultant>()))
            .Callback<Consultant>(c => createdConsultant = c)
            .ReturnsAsync((Consultant c) => { c.Id = 1; return c; });

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Branches).Returns(branchRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Consultants).Returns(consultantRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

        // Act
        await _consultantService.RegisterConsultantAsync(dto);

        // Assert
        Assert.NotNull(createdUser);
        Assert.False(createdUser!.IsActive);
        Assert.Equal(UserRole.Consultant, createdUser.Role);

        Assert.NotNull(createdConsultant);
        Assert.False(createdConsultant!.IsActive);
    }

    [Fact]
    public async Task ActivateConsultantAsync_WithValidId_ActivatesUserAndConsultant()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "john@example.com",
            IsActive = false,
            Role = UserRole.Consultant
        };

        var consultant = new Consultant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            BranchId = 1,
            UserId = 1,
            User = user,
            IsActive = false
        };

        var consultantRepoMock = new Mock<IRepository<Consultant>>();
        consultantRepoMock.Setup(r => r.GetByIdWithIncludesAsync(
            1, 
            It.IsAny<System.Linq.Expressions.Expression<Func<Consultant, object>>[]>()))
            .ReturnsAsync(consultant);
        consultantRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Consultant>()))
            .Returns(Task.CompletedTask);

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Consultants).Returns(consultantRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _consultantService.ActivateConsultantAsync(1);

        // Assert
        Assert.True(result);
        Assert.True(consultant.IsActive);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task ActivateConsultantAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var consultantRepoMock = new Mock<IRepository<Consultant>>();
        consultantRepoMock.Setup(r => r.GetByIdWithIncludesAsync(
            999, 
            It.IsAny<System.Linq.Expressions.Expression<Func<Consultant, object>>[]>()))
            .ReturnsAsync((Consultant?)null);

        _unitOfWorkMock.Setup(u => u.Consultants).Returns(consultantRepoMock.Object);

        // Act
        var result = await _consultantService.ActivateConsultantAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RejectConsultantAsync_WithValidId_DeletesConsultantAndUser()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "john@example.com",
            IsActive = false,
            Role = UserRole.Consultant
        };

        var consultant = new Consultant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            BranchId = 1,
            UserId = 1,
            User = user,
            IsActive = false
        };

        var consultantRepoMock = new Mock<IRepository<Consultant>>();
        consultantRepoMock.Setup(r => r.GetByIdWithIncludesAsync(
            1, 
            It.IsAny<System.Linq.Expressions.Expression<Func<Consultant, object>>[]>()))
            .ReturnsAsync(consultant);
        consultantRepoMock.Setup(r => r.DeleteAsync(It.IsAny<Consultant>()))
            .Returns(Task.CompletedTask);

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.DeleteAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Consultants).Returns(consultantRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _consultantService.RejectConsultantAsync(1);

        // Assert
        Assert.True(result);
        consultantRepoMock.Verify(r => r.DeleteAsync(consultant), Times.Once);
        userRepoMock.Verify(r => r.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task RejectConsultantAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var consultantRepoMock = new Mock<IRepository<Consultant>>();
        consultantRepoMock.Setup(r => r.GetByIdWithIncludesAsync(
            999, 
            It.IsAny<System.Linq.Expressions.Expression<Func<Consultant, object>>[]>()))
            .ReturnsAsync((Consultant?)null);

        _unitOfWorkMock.Setup(u => u.Consultants).Returns(consultantRepoMock.Object);

        // Act
        var result = await _consultantService.RejectConsultantAsync(999);

        // Assert
        Assert.False(result);
    }
}
