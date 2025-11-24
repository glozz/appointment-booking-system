using Microsoft.Extensions.Logging;
using Moq;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Application.Services;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<IActivityLogService> _activityLogMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _activityLogMock = new Mock<IActivityLogService>();
        _emailServiceMock = new Mock<IEmailService>();

        _userService = new UserService(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _activityLogMock.Object,
            _emailServiceMock.Object);
    }

    [Fact]
    public async Task GetProfileAsync_WithExistingUser_ReturnsProfile()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Phone = "+1234567890",
            Role = UserRole.User,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var sessionRepoMock = new Mock<IRepository<Session>>();
        sessionRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Session, bool>>>()))
            .ReturnsAsync(new List<Session>());

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Sessions).Returns(sessionRepoMock.Object);

        // Act
        var result = await _userService.GetProfileAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        // Act
        var result = await _userService.GetProfileAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidData_ReturnsUpdatedProfile()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Role = UserRole.User
        };

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Phone = "+9876543210"
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var sessionRepoMock = new Mock<IRepository<Session>>();
        sessionRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Session, bool>>>()))
            .ReturnsAsync(new List<Session>());

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Sessions).Returns(sessionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _userService.UpdateProfileAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("Smith", result.LastName);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ReturnsSessionsList()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session
            {
                Id = 1,
                UserId = 1,
                IpAddress = "192.168.1.1",
                UserAgent = "Test Browser",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            },
            new Session
            {
                Id = 2,
                UserId = 1,
                IpAddress = "192.168.1.2",
                UserAgent = "Another Browser",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            }
        };

        var sessionRepoMock = new Mock<IRepository<Session>>();
        sessionRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Session, bool>>>()))
            .ReturnsAsync(sessions);

        _unitOfWorkMock.Setup(u => u.Sessions).Returns(sessionRepoMock.Object);

        // Act
        var result = await _userService.GetActiveSessionsAsync(1, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Single(result, s => s.IsCurrentSession);
    }

    [Fact]
    public async Task RevokeSessionAsync_WithValidSession_ReturnsTrue()
    {
        // Arrange
        var session = new Session
        {
            Id = 1,
            UserId = 1,
            RevokedAt = null
        };

        var sessionRepoMock = new Mock<IRepository<Session>>();
        sessionRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Session, bool>>>()))
            .ReturnsAsync(new[] { session });
        sessionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Session>())).Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Sessions).Returns(sessionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _userService.RevokeSessionAsync(1, 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetUsersAsync_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var users = Enumerable.Range(1, 20).Select(i => new User
        {
            Id = i,
            FirstName = $"User{i}",
            LastName = "Test",
            Email = $"user{i}@example.com",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-i)
        }).ToList();

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        var search = new UserSearchDto
        {
            Page = 1,
            PageSize = 10,
            SortDescending = true
        };

        // Act
        var result = await _userService.GetUsersAsync(search);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetUsersAsync_WithSearchFilter_ReturnsFilteredResults()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com", Role = UserRole.User, IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", Role = UserRole.Admin, IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Id = 3, FirstName = "Bob", LastName = "Johnson", Email = "bob@example.com", Role = UserRole.User, IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        var search = new UserSearchDto
        {
            Search = "john",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _userService.GetUsersAsync(search);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount); // John Doe and Bob Johnson
    }

    [Fact]
    public async Task DeactivateUserAsync_WithExistingUser_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            IsActive = true
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _userService.DeactivateUserAsync(1);

        // Assert
        Assert.True(result);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task DeleteUserAsync_WithExistingUser_SoftDeletes()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            IsDeleted = false
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _userService.DeleteUserAsync(1);

        // Assert
        Assert.True(result);
        Assert.True(user.IsDeleted);
        Assert.NotNull(user.DeletedAt);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WithValidRole_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Role = UserRole.User
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _userService.UpdateUserRoleAsync(1, UserRole.Admin);

        // Assert
        Assert.True(result);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task UnlockUserAsync_WithLockedUser_UnlocksAndReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            FailedLoginAttempts = 5,
            LockoutEnd = DateTime.UtcNow.AddMinutes(10)
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _userService.UnlockUserAsync(1);

        // Assert
        Assert.True(result);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEnd);
    }
}
