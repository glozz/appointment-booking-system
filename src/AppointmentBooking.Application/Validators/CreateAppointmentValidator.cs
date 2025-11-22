using FluentValidation;
using AppointmentBooking.Application.DTOs;

namespace AppointmentBooking.Application.Validators;

/// <summary>
/// Validator for appointment creation requests
/// </summary>
public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentDto>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.BranchId)
            .GreaterThan(0)
            .WithMessage("Branch is required");

        RuleFor(x => x.ServiceId)
            .GreaterThan(0)
            .WithMessage("Service is required");

        RuleFor(x => x.AppointmentDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Appointment date cannot be in the past")
            .LessThanOrEqualTo(DateTime.Today.AddDays(30))
            .WithMessage("Appointments can only be booked up to 30 days in advance");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("Start time is required");

        // Customer validation
        RuleFor(x => x.Customer)
            .NotNull()
            .WithMessage("Customer information is required");

        RuleFor(x => x.Customer.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MaximumLength(50)
            .WithMessage("First name cannot exceed 50 characters");

        RuleFor(x => x.Customer.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MaximumLength(50)
            .WithMessage("Last name cannot exceed 50 characters");

        RuleFor(x => x.Customer.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(100)
            .WithMessage("Email cannot exceed 100 characters");

        RuleFor(x => x.Customer.Phone)
            .NotEmpty()
            .WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Invalid phone number format. Use international format (e.g., +1234567890)")
            .MaximumLength(20)
            .WithMessage("Phone number cannot exceed 20 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
