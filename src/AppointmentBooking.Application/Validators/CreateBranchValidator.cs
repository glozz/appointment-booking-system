using FluentValidation;
using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Application.Validators;

/// <summary>
/// Validator for branch creation requests
/// </summary>
public class CreateBranchValidator : AbstractValidator<CreateBranchDto>
{
    public CreateBranchValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Branch name is required")
            .MaximumLength(100)
            .WithMessage("Branch name cannot exceed 100 characters");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Address is required")
            .MaximumLength(200)
            .WithMessage("Address cannot exceed 200 characters");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required")
            .MaximumLength(100)
            .WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Invalid phone number format. Use international format (e.g., +1234567890)")
            .MaximumLength(20)
            .WithMessage("Phone number cannot exceed 20 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(100)
            .WithMessage("Email cannot exceed 100 characters");
    }
}
