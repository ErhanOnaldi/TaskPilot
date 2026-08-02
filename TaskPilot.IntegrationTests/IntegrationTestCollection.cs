namespace TaskPilot.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TaskPilotIntegrationTestCollection : ICollectionFixture<TaskPilotIntegrationEnvironment>
{
    public const string Name = "taskpilot-integration";
}
