using TaskPilot.Application.Features.Project.Dtos;
using TaskPilot.Application.Features.Project.Validators;
using TaskPilot.Application.Features.Notifications.Dtos;
using TaskPilot.Application.Features.Notifications.Validators;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Features.Tasks.Validators;
using TaskPilot.Application.Features.Workspace.Dtos;
using TaskPilot.Application.Features.Workspace.Validators;

namespace TaskPilot.Application.Tests;

public class PaginationQueryValidatorTests
{
    [Fact]
    public void TaskQueryParametersValidator_rejects_invalid_pagination_and_search()
    {
        var validator = new TaskQueryParametersValidator();
        var query = new TaskQueryParameters
        {
            PageNumber = 0,
            PageSize = 101,
            Search = new string('a', 101)
        };

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(TaskQueryParameters.PageNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(TaskQueryParameters.PageSize));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(TaskQueryParameters.Search));
    }

    [Fact]
    public void ProjectQueryParametersValidator_accepts_default_query()
    {
        var validator = new ProjectQueryParametersValidator();

        var result = validator.Validate(new ProjectQueryParameters());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void WorkspaceQueryParametersValidator_rejects_invalid_pagination()
    {
        var validator = new WorkspaceQueryParametersValidator();
        var query = new WorkspaceQueryParameters
        {
            PageNumber = -1,
            PageSize = 0
        };

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(WorkspaceQueryParameters.PageNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(WorkspaceQueryParameters.PageSize));
    }

    [Fact]
    public void NotificationQueryParametersValidator_rejects_invalid_pagination_and_type()
    {
        var validator = new NotificationQueryParametersValidator();
        var query = new NotificationQueryParameters
        {
            PageNumber = 0,
            PageSize = 101,
            Type = new string('a', 101)
        };

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(NotificationQueryParameters.PageNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(NotificationQueryParameters.PageSize));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(NotificationQueryParameters.Type));
    }
}
