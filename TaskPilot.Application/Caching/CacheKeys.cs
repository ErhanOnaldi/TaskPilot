namespace TaskPilot.Application.Caching;

public static class CacheKeys
{
    public static string ProjectDashboard(int projectId)
    {
        return $"project-dashboard:{projectId}";
    }
}