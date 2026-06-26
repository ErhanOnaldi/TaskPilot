using FluentValidation;
using TaskPilot.Application.Features.Tasks.Dtos;

namespace TaskPilot.Application.Features.Tasks.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Task title is required.")
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("Task title is required.")
            .MaximumLength(100).WithMessage("Task title must be at most 100 characters.");

        RuleFor(x => x.Priority)
            .IsInEnum().When(x => x.Priority.HasValue).WithMessage("Task priority is invalid.");

        RuleFor(x => x.DueDate)
            .Must(dueDate => dueDate is null || dueDate.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage("DueDate cannot be in the past.");
    }
}
