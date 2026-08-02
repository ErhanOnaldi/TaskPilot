using FluentValidation;
using TaskPilot.Application.Features.Knowledge.Dtos;

namespace TaskPilot.Application.Features.Knowledge.Validators;

public sealed class CreateKnowledgeTagRequestValidator : AbstractValidator<CreateKnowledgeTagRequest>
{
    public CreateKnowledgeTagRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Slug).NotEmpty().MaximumLength(100);
    }
}
