using FluentValidation;
using TaskPilot.Application.Features.Knowledge.Dtos;

namespace TaskPilot.Application.Features.Knowledge.Validators;

public sealed class UpdateKnowledgeFolderRequestValidator : AbstractValidator<UpdateKnowledgeFolderRequest>
{
    public UpdateKnowledgeFolderRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Slug).NotEmpty().MaximumLength(160);
    }
}
