using FluentValidation;
using TaskPilot.Application.Features.Auth.Dtos;

namespace TaskPilot.Application.Features.Auth.Validators;

public sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Google id token is required.")
            .MaximumLength(4096).WithMessage("Google id token is too long.");
    }
}
