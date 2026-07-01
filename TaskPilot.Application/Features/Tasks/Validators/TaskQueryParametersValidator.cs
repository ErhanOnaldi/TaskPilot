using FluentValidation;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Tasks.Dtos;

namespace TaskPilot.Application.Features.Tasks.Validators;

public sealed class TaskQueryParametersValidator : AbstractValidator<TaskQueryParameters>
{
    public TaskQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginationRequest.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationRequest.MaxPageSize}.");

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue).WithMessage("Task status is invalid.");

        RuleFor(x => x.Priority)
            .IsInEnum().When(x => x.Priority.HasValue).WithMessage("Task priority is invalid.");

        RuleFor(x => x.SortBy)
            .IsInEnum().WithMessage("SortBy is invalid.");

        RuleFor(x => x.SortDirection)
            .IsInEnum().WithMessage("SortDirection is invalid.");

        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Search must be at most 100 characters.");
    }
}
