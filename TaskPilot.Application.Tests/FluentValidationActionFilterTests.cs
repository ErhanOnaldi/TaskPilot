using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.API.Filters;
using TaskPilot.Application.Features.Workspace.Dtos;
using TaskPilot.Application.Features.Workspace.Validators;

namespace TaskPilot.Application.Tests;

public class FluentValidationActionFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_returns_validation_problem_and_does_not_call_action_when_request_is_invalid()
    {
        var filter = new FluentValidationActionFilter();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IValidator<CreateWorkspaceRequest>, CreateWorkspaceRequestValidator>()
            .BuildServiceProvider();
        var context = CreateContext(
            serviceProvider,
            new Dictionary<string, object?>
            {
                ["request"] = new CreateWorkspaceRequest("")
            });
        var actionWasCalled = false;

        await filter.OnActionExecutionAsync(
            context,
            () =>
            {
                actionWasCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

        Assert.False(actionWasCalled);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Contains(nameof(CreateWorkspaceRequest.Name), problemDetails.Errors.Keys);
    }

    [Fact]
    public async Task OnActionExecutionAsync_calls_action_when_request_is_valid()
    {
        var filter = new FluentValidationActionFilter();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IValidator<CreateWorkspaceRequest>, CreateWorkspaceRequestValidator>()
            .BuildServiceProvider();
        var context = CreateContext(
            serviceProvider,
            new Dictionary<string, object?>
            {
                ["request"] = new CreateWorkspaceRequest("Engineering")
            });
        var actionWasCalled = false;

        await filter.OnActionExecutionAsync(
            context,
            () =>
            {
                actionWasCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

        Assert.True(actionWasCalled);
        Assert.Null(context.Result);
    }

    private static ActionExecutingContext CreateContext(
        IServiceProvider serviceProvider,
        IDictionary<string, object?> actionArguments)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/api/workspaces";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            [],
            actionArguments,
            controller: new object());
    }

    private static ActionExecutedContext CreateExecutedContext(ActionExecutingContext context)
    {
        return new ActionExecutedContext(
            context,
            context.Filters,
            context.Controller);
    }
}
