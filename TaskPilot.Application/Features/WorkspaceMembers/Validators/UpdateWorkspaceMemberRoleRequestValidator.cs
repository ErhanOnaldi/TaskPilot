using FluentValidation;
using TaskPilot.Application.Features.WorkspaceMembers.Dtos;

namespace TaskPilot.Application.Features.WorkspaceMembers.Validators;

public sealed class UpdateWorkspaceMemberRoleRequestValidator : AbstractValidator<UpdateWorkspaceMemberRoleRequest>
{
    public UpdateWorkspaceMemberRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Workspace member role is invalid.");
    }
}
