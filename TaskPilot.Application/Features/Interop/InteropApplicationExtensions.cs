using Microsoft.Extensions.DependencyInjection;

namespace TaskPilot.Application.Features.Interop;

public static class InteropApplicationExtensions
{
    public static IServiceCollection AddInteropApplication(this IServiceCollection services) => services
        .AddScoped<IInteropToolCatalog, InteropToolCatalog>()
        .AddScoped<IA2aService, A2aService>();
}
