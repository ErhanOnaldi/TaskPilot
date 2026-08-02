using System.Reflection;
using TaskPilot.API.Controllers;
using TaskPilot.Application;
using TaskPilot.Domain.Entities;
using TaskPilot.Infrastructure;
using TaskPilot.Persistence;

namespace TaskPilot.Application.Tests;

public sealed class ArchitectureDependencyTests
{
    [Fact]
    public void Domain_does_not_reference_other_TaskPilot_layers()
    {
        AssertTaskPilotReferences(typeof(User).Assembly);
    }

    [Fact]
    public void Application_only_references_Domain()
    {
        AssertTaskPilotReferences(typeof(ServiceResult).Assembly, "TaskPilot.Domain");
    }

    [Fact]
    public void Application_does_not_reference_Microsoft_Extensions_AI()
    {
        Assert.DoesNotContain(
            typeof(ServiceResult).Assembly.GetReferencedAssemblies(),
            reference => string.Equals(reference.Name, "Microsoft.Extensions.AI.Abstractions", StringComparison.Ordinal) ||
                         string.Equals(reference.Name, "Microsoft.Extensions.AI", StringComparison.Ordinal));
    }

    [Fact]
    public void Persistence_only_references_Application_and_Domain()
    {
        AssertTaskPilotReferences(
            typeof(AppDbContext).Assembly,
            "TaskPilot.Application",
            "TaskPilot.Domain");
    }

    [Fact]
    public void Infrastructure_only_references_Application_and_Domain()
    {
        AssertTaskPilotReferences(
            typeof(CurrentUserService).Assembly,
            "TaskPilot.Application",
            "TaskPilot.Domain");
    }

    [Fact]
    public void Controllers_do_not_depend_on_persistence_implementations()
    {
        var controllerTypes = typeof(CustomBaseController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && type.Namespace == typeof(CustomBaseController).Namespace)
            .ToArray();

        var forbiddenDependencies = controllerTypes
            .SelectMany(type => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Where(type => type.Namespace?.StartsWith("TaskPilot.Persistence", StringComparison.Ordinal) == true)
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(forbiddenDependencies);
    }

    private static void AssertTaskPilotReferences(Assembly assembly, params string[] allowedReferences)
    {
        var actualReferences = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name?.StartsWith("TaskPilot.", StringComparison.Ordinal) == true)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(allowedReferences.OrderBy(name => name), actualReferences);
    }
}
