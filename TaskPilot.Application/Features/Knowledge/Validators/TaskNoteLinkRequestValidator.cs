using FluentValidation;
using TaskPilot.Application.Features.Knowledge.Dtos;

namespace TaskPilot.Application.Features.Knowledge.Validators;

public sealed class TaskNoteLinkRequestValidator : AbstractValidator<TaskNoteLinkRequest>
{
    public TaskNoteLinkRequestValidator()
    {
        RuleFor(request => request.TaskId).GreaterThan(0);
        RuleFor(request => request.NoteId).GreaterThan(0);
    }
}
