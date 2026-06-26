using FluentValidation;
using TaskPilot.Application.Features.ProjectMembers.Dtos;

namespace TaskPilot.Application.Features.ProjectMembers.Validators;

public sealed class UpdateProjectMemberRoleRequestValidator : AbstractValidator<UpdateProjectMemberRoleRequest>
{
    public UpdateProjectMemberRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Project member role is invalid.");
    }
}
