using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Application.Services;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces;

namespace AppointmentBooking.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<IActivityLogService> _activityLogMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _activityLogMock = new Mock<IActivityLogService>();
        _emailServiceMock = new Mock<IEmailService>();

        // Setup configuration
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns("TestSecretKeyThatIsAtLeast32CharactersLong!");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _configurationMock.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("60");
        _configurationMock.Setup(c => c["App:BaseUrl"]).Returns("http://localhost:5000");

        _authService = new AuthService(
            _unitOfWorkMock.Object,
            _configurationMock.Object,
            _loggerMock.Object,
            _activityLogMock.Object,
            _emailServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<User>());
        userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var sessionRepoMock = new Mock<IRepository<Session>>();
        sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<Session>()))
            .ReturnsAsync((Session s) => s);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Sessions).Returns(sessionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.NotNull(result.User);
        Assert.Equal("john.doe@example.com", result.User.Email);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var existingUser = new User { Email = "existing@example.com" };
        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new[] { existingUser });

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Email address is already registered.", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_WithWeakPassword_ReturnsFailure()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "weak",  // Too short, no uppercase, no special char
            ConfirmPassword = "weak"
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<User>());

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Password must be at least 8 characters", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
            IsActive = true,
            Role = UserRole.User
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new[] { user });
        userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        var sessionRepoMock = new Mock<IRepository<Session>>();
        sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<Session>()))
            .ReturnsAsync((Session s) => s);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Sessions).Returns(sessionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
            IsActive = true
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new[] { user });
        userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithDeactivatedAccount_ReturnsFailure()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
            IsActive = false
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new[] { user });

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Account has been deactivated.", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithLockedAccount_ReturnsFailure()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
            IsActive = true,
            LockoutEnd = DateTime.UtcNow.AddMinutes(10)
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new[] { user });

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Account is locked", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithPendingConsultantAccount_ReturnsPendingApprovalMessage()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "consultant@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = 1,
            Email = "consultant@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
            IsActive = false,
            Role = UserRole.Consultant // This is a consultant account pending approval
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new[] { user });

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("pending admin approval", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var token = "valid-token";
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            EmailVerificationToken = token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1),
            EmailVerified = false
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(new[] { user });
        userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authService.VerifyEmailAsync(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<User>());

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        // Act
        var result = await _authService.VerifyEmailAsync("invalid-token");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ReturnsTrue()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!", 12)
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authService.ChangePasswordAsync(1, changePasswordDto);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ReturnsFalse()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!", 12)
        };

        var userRepoMock = new Mock<IRepository<User>>();
        userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        _unitOfWorkMock.Setup(u => u.Users).Returns(userRepoMock.Object);

        // Act
        var result = await _authService.ChangePasswordAsync(1, changePasswordDto);

        // Assert
        Assert.False(result);
    }
}
