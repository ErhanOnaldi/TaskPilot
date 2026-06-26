using FluentValidation;
using TaskPilot.Application.Features.Tasks.Dtos;

namespace TaskPilot.Application.Features.Tasks.Validators;

public sealed class AssignTaskRequestValidator : AbstractValidator<AssignTaskRequest>
{
    public AssignTaskRequestValidator()
    {
        RuleFor(x => x.AssignedUserId)
            .GreaterThan(0)
            .When(x => x.AssignedUserId.HasValue)
            .WithMessage("AssignedUserId must be greater than 0.");
    }
}
