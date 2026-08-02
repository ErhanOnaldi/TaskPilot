using System.Text.Json;
using TaskPilot.Application.Features.Interop;

namespace TaskPilot.Application.Tests;

public sealed class InteropSemanticTests
{
    [Fact]
    public void Tool_catalog_is_narrow_and_contains_no_destructive_tools()
    {
        var names = typeof(InteropToolCatalog).GetField("Definitions", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null) as IReadOnlyList<InteropToolDefinition>;
        Assert.NotNull(names);
        Assert.Contains(names!, tool => tool.Name == "tasks.create");
        Assert.DoesNotContain(names!, tool => tool.Name.Contains("delete", StringComparison.OrdinalIgnoreCase) || tool.Name.Contains("archive", StringComparison.OrdinalIgnoreCase));
    }
}
