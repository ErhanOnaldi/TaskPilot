using FluentValidation;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Notifications.Dtos;

namespace TaskPilot.Application.Features.Notifications.Validators;

public sealed class NotificationQueryParametersValidator : AbstractValidator<NotificationQueryParameters>
{
    public NotificationQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginationRequest.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationRequest.MaxPageSize}.");

        RuleFor(x => x.SortBy)
            .IsInEnum().WithMessage("SortBy is invalid.");

        RuleFor(x => x.SortDirection)
            .IsInEnum().WithMessage("SortDirection is invalid.");

        RuleFor(x => x.Type)
            .MaximumLength(100).WithMessage("Type must be at most 100 characters.");
    }
}
