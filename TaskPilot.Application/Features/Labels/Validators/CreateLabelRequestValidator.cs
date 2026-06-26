using FluentValidation;
using TaskPilot.Application.Features.Labels.Dtos;

namespace TaskPilot.Application.Features.Labels.Validators;

public sealed class CreateLabelRequestValidator : AbstractValidator<CreateLabelRequest>
{
    public CreateLabelRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Label name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Label name is required.")
            .MaximumLength(100).WithMessage("Label name must be at most 100 characters.");

        RuleFor(x => x.Color)
            .MaximumLength(50)
            .When(x => x.Color is not null)
            .WithMessage("Label color must be at most 50 characters.");
    }
}
