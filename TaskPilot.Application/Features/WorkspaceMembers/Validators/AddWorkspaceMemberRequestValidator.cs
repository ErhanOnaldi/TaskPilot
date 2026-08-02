using FluentValidation;
using TaskPilot.Application.Features.WorkspaceMembers.Dtos;

namespace TaskPilot.Application.Features.WorkspaceMembers.Validators;

#pragma warning disable CS0618 // Validate the temporary legacy UserId contract during its deprecation window.
public sealed class AddWorkspaceMemberRequestValidator : AbstractValidator<AddWorkspaceMemberRequest>
{
    public AddWorkspaceMemberRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || x.UserId.HasValue)
            .WithMessage("Email is required.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().When(x => !x.UserId.HasValue)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.UserId)
            .GreaterThan(0).When(x => x.UserId.HasValue)
            .WithMessage("UserId must be greater than 0.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Workspace member role is invalid.");
    }
}
#pragma warning restore CS0618
