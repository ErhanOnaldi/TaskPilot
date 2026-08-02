using FluentValidation;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;

namespace TaskPilot.Application.Features.Tasks.Validators;

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Task title is required.")
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("Task title is required.")
            .MaximumLength(100).WithMessage("Task title must be at most 100 characters.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Task priority is invalid.");

        RuleFor(x => x.DueDate)
            .Must(dueDate => dueDate is null || dueDate.Value.Date >= dateTimeProvider.UtcNow.Date)
            .WithMessage("DueDate cannot be in the past.");
    }
}
