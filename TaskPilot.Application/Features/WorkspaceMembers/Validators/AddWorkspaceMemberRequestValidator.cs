using FluentValidation;
using TaskPilot.Application.Features.WorkspaceMembers.Dtos;

namespace TaskPilot.Application.Features.WorkspaceMembers.Validators;

public sealed class AddWorkspaceMemberRequestValidator : AbstractValidator<AddWorkspaceMemberRequest>
{
    public AddWorkspaceMemberRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be greater than 0.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Workspace member role is invalid.");
    }
}
