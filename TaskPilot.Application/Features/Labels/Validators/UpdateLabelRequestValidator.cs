using FluentValidation;
using TaskPilot.Application.Features.Labels.Dtos;
using TaskPilot.Domain.Policies;

namespace TaskPilot.Application.Features.Labels.Validators;

public sealed class UpdateLabelRequestValidator : AbstractValidator<UpdateLabelRequest>
{
    public UpdateLabelRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Label name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Label name is required.")
            .MaximumLength(LabelPolicy.MaximumNameCharacters).WithMessage("Label name must be at most 50 characters.");

        RuleFor(x => x.Color)
            .MaximumLength(50)
            .When(x => x.Color is not null)
            .WithMessage("Label color must be at most 50 characters.");
    }
}
