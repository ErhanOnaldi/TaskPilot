using FluentValidation;
using TaskPilot.Application.Features.Tasks.Dtos;

namespace TaskPilot.Application.Features.Tasks.Validators;

public sealed class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Task status is invalid.");
    }
}
