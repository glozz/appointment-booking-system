using FluentValidation;
using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Application.Validators;

public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
            .Must(HaveUppercase).WithMessage("Password must contain at least one uppercase letter.")
            .Must(HaveLowercase).WithMessage("Password must contain at least one lowercase letter.")
            .Must(HaveDigit).WithMessage("Password must contain at least one number.")
            .Must(HaveSpecialChar).WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm your password.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.")
            .Matches(@"^[+]?[\d\s\-()]+$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Invalid phone number format.");
    }

    private static bool HaveUppercase(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsUpper);

    private static bool HaveLowercase(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsLower);

    private static bool HaveDigit(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsDigit);

    private static bool HaveSpecialChar(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(c => !char.IsLetterOrDigit(c));
}

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
            .Must(HaveUppercase).WithMessage("Password must contain at least one uppercase letter.")
            .Must(HaveLowercase).WithMessage("Password must contain at least one lowercase letter.")
            .Must(HaveDigit).WithMessage("Password must contain at least one number.")
            .Must(HaveSpecialChar).WithMessage("Password must contain at least one special character.")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Please confirm your new password.")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }

    private static bool HaveUppercase(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsUpper);

    private static bool HaveLowercase(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsLower);

    private static bool HaveDigit(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsDigit);

    private static bool HaveSpecialChar(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(c => !char.IsLetterOrDigit(c));
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
            .Must(HaveUppercase).WithMessage("Password must contain at least one uppercase letter.")
            .Must(HaveLowercase).WithMessage("Password must contain at least one lowercase letter.")
            .Must(HaveDigit).WithMessage("Password must contain at least one number.")
            .Must(HaveSpecialChar).WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm your password.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }

    private static bool HaveUppercase(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsUpper);

    private static bool HaveLowercase(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsLower);

    private static bool HaveDigit(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(char.IsDigit);

    private static bool HaveSpecialChar(string password) => 
        !string.IsNullOrEmpty(password) && password.Any(c => !char.IsLetterOrDigit(c));
}

public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.")
            .Matches(@"^[+]?[\d\s\-()]+$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Invalid phone number format.");
    }
}
