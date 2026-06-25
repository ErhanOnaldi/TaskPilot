using FluentValidation;
using TaskPilot.Application.Features.Project.Dtos;

namespace TaskPilot.Application.Features.Project.Validators;

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Project name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Project name is required.")
            .MaximumLength(100).WithMessage("Project name must be at most 100 characters.");
        
        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description is not null);
    }
}