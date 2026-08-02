using Microsoft.Extensions.DependencyInjection;

namespace TaskPilot.Infrastructure.Features.Interop;

public static class InteropInfrastructureExtensions
{
    public static IServiceCollection AddInteropInfrastructure(this IServiceCollection services) => services.AddSingleton<IInteropFeatureGate, InteropFeatureGate>();
}
