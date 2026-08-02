using Microsoft.Extensions.Configuration;

namespace TaskPilot.Infrastructure.Features.Interop;

public interface IInteropFeatureGate { bool McpEnabled { get; } bool A2aEnabled { get; } }
public sealed class InteropFeatureGate(IConfiguration configuration) : IInteropFeatureGate
{
    public bool McpEnabled => configuration.GetValue<bool>("Features:Interop:McpEnabled");
    public bool A2aEnabled => configuration.GetValue<bool>("Features:Interop:A2aEnabled");
}
