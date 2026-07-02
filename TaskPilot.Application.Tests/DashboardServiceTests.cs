using System.Net;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Authorization.Results;
using TaskPilot.Application.Features.Dashboard.Dtos;
using TaskPilot.Application.Features.Dashboard.Services;
using TaskPilot.Application.Interfaces.Infrastructure.Caching;
using TaskPilot.Application.Interfaces.Persistence.Dashboard;

namespace TaskPilot.Application.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetProjectDashboardAsync_returns_repository_aggregate_for_authorized_user()
    {
        var repository = new FakeDashboardRepository(
            new ProjectDashboardResponse(
                ProjectId: 20,
                TotalTasks: 8,
                TodoTasks: 1,
                InProgressTasks: 2,
                InReviewTasks: 1,
                DoneTasks: 2,
                CancelledTasks: 1,
                OverdueTasks: 1,
                AssignedTasks: 6,
                UnassignedTasks: 2));
        var service = new DashboardService(repository, new FakeAccessControlService(), new FakeCacheService());

        var result = await service.GetProjectDashboardAsync(20, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(8, result.Data.TotalTasks);
        Assert.Equal(1, result.Data.OverdueTasks);
        Assert.Equal(20, repository.ProjectId);
    }

    [Fact]
    public async Task GetProjectDashboardAsync_does_not_query_repository_when_access_fails()
    {
        var repository = new FakeDashboardRepository(new ProjectDashboardResponse(20, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        var service = new DashboardService(
            repository,
            new FakeAccessControlService(ServiceResult.Fail("Project not found.", HttpStatusCode.NotFound)),
            new FakeCacheService());

        var result = await service.GetProjectDashboardAsync(20, CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.NotFound, result.Status);
        Assert.False(repository.WasCalled);
    }

    private sealed class FakeDashboardRepository(ProjectDashboardResponse response) : IDashboardRepository
    {
        public bool WasCalled { get; private set; }
        public int ProjectId { get; private set; }

        public Task<ProjectDashboardResponse> GetProjectDashboardAsync(
            int projectId,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            ProjectId = projectId;
            return Task.FromResult(response);
        }
    }

    private sealed class FakeAccessControlService(ServiceResult? failure = null) : IAccessControlService
    {
        public Task<WorkspaceAccessResult> AuthorizeWorkspaceAsync(
            int workspaceId,
            WorkspaceAccessLevel accessLevel,
            bool requireActiveWorkspace,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ProjectAccessResult> AuthorizeProjectAsync(
            int projectId,
            ProjectAccessLevel accessLevel,
            bool requireActiveProject,
            CancellationToken cancellationToken)
        {
            var result = failure is null
                ? new ProjectAccessResult(null!, null!, null!, 1, null)
                : ProjectAccessResult.Fail(failure, 1);

            return Task.FromResult(result);
        }
    }

    private sealed class FakeCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        {
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
