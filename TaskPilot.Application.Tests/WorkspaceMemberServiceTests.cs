using System.Linq.Expressions;
using System.Net;
using TaskPilot.Application;
using TaskPilot.Application.Features.WorkspaceMembers.Dtos;
using TaskPilot.Application.Features.WorkspaceMembers.Services;
using TaskPilot.Application.Features.WorkspaceMembers.Validators;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.User;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Tests;

public class WorkspaceMemberServiceTests
{
    [Fact]
    public async Task AddMemberAsync_adds_user_when_owner_and_user_is_not_member()
    {
        var workspaceRepository = new FakeWorkspaceRepository();
        workspaceRepository.Workspaces.Add(new WorkSpace { Id = 10, Name = "Engineering" });
        var memberRepository = new FakeWorkspaceMemberRepository();
        memberRepository.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 1, Role = Role.Owner });
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(new User { Id = 2, Email = "member@example.com", PasswordHash = "hash" });
        var service = CreateService(workspaceRepository, memberRepository, userRepository, currentUserId: 1);

        var result = await service.AddMemberAsync(
            10,
            new AddWorkspaceMemberRequest(2, Role.Member),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data?.UserId);
        Assert.Contains(memberRepository.Members, member => member.WorkspaceId == 10 && member.UserId == 2);
    }

    [Fact]
    public async Task AddMemberAsync_returns_conflict_when_user_is_already_member()
    {
        var workspaceRepository = new FakeWorkspaceRepository();
        workspaceRepository.Workspaces.Add(new WorkSpace { Id = 10, Name = "Engineering" });
        var memberRepository = new FakeWorkspaceMemberRepository();
        memberRepository.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 1, Role = Role.Owner });
        memberRepository.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member });
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(new User { Id = 2, Email = "member@example.com", PasswordHash = "hash" });
        var service = CreateService(workspaceRepository, memberRepository, userRepository, currentUserId: 1);

        var result = await service.AddMemberAsync(
            10,
            new AddWorkspaceMemberRequest(2, Role.Member),
            CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.Conflict, result.Status);
        Assert.Single(memberRepository.Members, member => member.WorkspaceId == 10 && member.UserId == 2);
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_returns_bad_request_when_workspace_is_archived()
    {
        var workspaceRepository = new FakeWorkspaceRepository();
        workspaceRepository.Workspaces.Add(new WorkSpace { Id = 10, Name = "Engineering", IsArchived = true });
        var memberRepository = new FakeWorkspaceMemberRepository();
        memberRepository.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 1, Role = Role.Owner });
        memberRepository.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member });
        var service = CreateService(workspaceRepository, memberRepository, new FakeUserRepository(), currentUserId: 1);

        var result = await service.UpdateMemberRoleAsync(
            10,
            2,
            new UpdateWorkspaceMemberRoleRequest(Role.Manager),
            CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(Role.Member, memberRepository.Members.Single(member => member.UserId == 2).Role);
    }

    private static WorkspaceMemberService CreateService(
        FakeWorkspaceRepository workspaceRepository,
        FakeWorkspaceMemberRepository memberRepository,
        FakeUserRepository userRepository,
        int currentUserId)
    {
        return new WorkspaceMemberService(
            new FakeUnitOfWork(),
            new FakeCurrentUserService(currentUserId),
            workspaceRepository,
            userRepository,
            memberRepository,
            new AddWorkspaceMemberRequestValidator(),
            new UpdateWorkspaceMemberRoleRequestValidator());
    }

    private sealed class FakeCurrentUserService(int userId) : ICurrentUserService
    {
        public int UserId { get; } = userId;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeWorkspaceRepository : IWorkspaceRepository
    {
        public List<WorkSpace> Workspaces { get; } = [];

        public Task<List<WorkSpace>> GetWorkspacesByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Workspaces.Where(workspace => workspace.Members.Any(member => member.UserId == userId)).ToList());
        }

        public Task<WorkSpace?> GetWorkspaceForMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Workspaces.FirstOrDefault(workspace => workspace.Id == workspaceId));
        }

        public Task<WorkSpace?> GetWorkspaceForOwnerAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Workspaces.FirstOrDefault(workspace => workspace.Id == workspaceId));
        }

        public Task<List<WorkSpace>> GetAllAsync() => Task.FromResult(Workspaces);
        public Task<List<WorkSpace>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Workspaces);
        public IQueryable<WorkSpace> Where(Expression<Func<WorkSpace, bool>> predicate) => Workspaces.AsQueryable().Where(predicate);
        public ValueTask<WorkSpace?> GetByIdAsync(int id) => ValueTask.FromResult(Workspaces.FirstOrDefault(workspace => workspace.Id == id));
        public ValueTask AddAsync(WorkSpace entity) { Workspaces.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<WorkSpace, bool>> predicate) => Task.FromResult(Workspaces.AsQueryable().Any(predicate));
        public void Update(WorkSpace entity) { }
        public void Delete(WorkSpace entity) => Workspaces.Remove(entity);
    }

    private sealed class FakeWorkspaceMemberRepository : IWorkspaceMemberRepository
    {
        public List<WorkspaceMember> Members { get; } = [];

        public Task<List<WorkspaceMember>> GetMembersByWorkspaceIdAsync(int workspaceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Members.Where(member => member.WorkspaceId == workspaceId).ToList());
        }

        public Task<WorkspaceMember?> GetMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Members.FirstOrDefault(member => member.WorkspaceId == workspaceId && member.UserId == userId));
        }

        public Task<bool> IsWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Members.Any(member => member.WorkspaceId == workspaceId && member.UserId == userId));
        }

        public Task<bool> IsWorkspaceOwnerAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Members.Any(member => member.WorkspaceId == workspaceId && member.UserId == userId && member.Role == Role.Owner));
        }

        public Task<int> CountOwnersAsync(int workspaceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Members.Count(member => member.WorkspaceId == workspaceId && member.Role == Role.Owner));
        }

        public Task<List<WorkspaceMember>> GetAllAsync() => Task.FromResult(Members);
        public Task<List<WorkspaceMember>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Members);
        public IQueryable<WorkspaceMember> Where(Expression<Func<WorkspaceMember, bool>> predicate) => Members.AsQueryable().Where(predicate);
        public ValueTask<WorkspaceMember?> GetByIdAsync(int id) => ValueTask.FromResult(Members.FirstOrDefault(member => member.Id == id));
        public ValueTask AddAsync(WorkspaceMember entity) { Members.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<WorkspaceMember, bool>> predicate) => Task.FromResult(Members.AsQueryable().Any(predicate));
        public void Update(WorkspaceMember entity) { }
        public void Delete(WorkspaceMember entity) => Members.Remove(entity);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.Email == email));
        }

        public Task<List<User>> GetAllAsync() => Task.FromResult(Users);
        public Task<List<User>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Users);
        public IQueryable<User> Where(Expression<Func<User, bool>> predicate) => Users.AsQueryable().Where(predicate);
        public ValueTask<User?> GetByIdAsync(int id) => ValueTask.FromResult(Users.FirstOrDefault(user => user.Id == id));
        public ValueTask AddAsync(User entity) { Users.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<User, bool>> predicate) => Task.FromResult(Users.AsQueryable().Any(predicate));
        public void Update(User entity) { }
        public void Delete(User entity) => Users.Remove(entity);
    }
}
