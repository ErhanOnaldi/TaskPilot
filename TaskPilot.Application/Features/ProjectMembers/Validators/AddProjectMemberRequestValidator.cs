using FluentValidation;
using TaskPilot.Application.Features.ProjectMembers.Dtos;

namespace TaskPilot.Application.Features.ProjectMembers.Validators;

public sealed class AddProjectMemberRequestValidator : AbstractValidator<AddProjectMemberRequest>
{
    public AddProjectMemberRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be greater than 0.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Project member role is invalid.");
    }
}
