using FluentValidation;
using TaskPilot.Application.Features.Knowledge.Dtos;

namespace TaskPilot.Application.Features.Knowledge.Validators;

public sealed class UpdateNoteRequestValidator : AbstractValidator<UpdateNoteRequest>
{
    public UpdateNoteRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Slug).NotEmpty().MaximumLength(160);
        RuleFor(request => request.Content).MaximumLength(100_000).When(request => request.Content is not null);
        RuleFor(request => request.ExpectedVersion).GreaterThan(0);
    }
}
