using FluentValidation;
using TaskPilot.Application.Features.Workspace.Dtos;

namespace TaskPilot.Application.Features.Workspace.Validators;

public sealed class CreateWorkspaceRequestValidator : AbstractValidator<CreateWorkspaceRequest>
{
    public CreateWorkspaceRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Workspace name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Workspace name is required.")
            .MaximumLength(100).WithMessage("Workspace name must be at most 100 characters.");
    }
}
