using FluentValidation;
using TaskPilot.Application.Features.Comments.Dtos;

namespace TaskPilot.Application.Features.Comments.Validators;

public sealed class UpdateCommentRequestValidator : AbstractValidator<UpdateCommentRequest>
{
    public UpdateCommentRequestValidator()
    {
        RuleFor(x => x.Content)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Comment content is required.")
            .Must(content => !string.IsNullOrWhiteSpace(content)).WithMessage("Comment content is required.")
            .MaximumLength(4000).WithMessage("Comment content must be at most 4000 characters.");
    }
}
