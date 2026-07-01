using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Interfaces.Persistence.Tasks;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public sealed class TaskRepository : GenericRepository<TaskItem>, ITaskRepository
{
    private readonly AppDbContext _dbContext;

    public TaskRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<TaskItem>> GetTasksByProjectIdAsync(
        int projectId,
        TaskQueryParameters query,
        CancellationToken cancellationToken)
    {
        var taskQuery = _dbContext.TaskItems
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId);

        if (query.Status.HasValue)
        {
            taskQuery = taskQuery.Where(task => task.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            taskQuery = taskQuery.Where(task => task.Priority == query.Priority.Value);
        }

        if (query.AssignedUserId.HasValue)
        {
            taskQuery = taskQuery.Where(task => task.AssignedUserId == query.AssignedUserId.Value);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            taskQuery = taskQuery.Where(task =>
                task.Title.Contains(search) ||
                (task.Description != null && task.Description.Contains(search)));
        }

        var totalCount = await taskQuery.CountAsync(cancellationToken);
        var items = await ApplySorting(taskQuery, query)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResponse<TaskItem>.Create(items, query.PageNumber, query.PageSize, totalCount);
    }

    private static IOrderedQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, TaskQueryParameters parameters)
    {
        return (parameters.SortBy, parameters.SortDirection) switch
        {
            (TaskSortBy.DueDate, SortDirection.Asc) => query.OrderBy(task => task.DueDate).ThenByDescending(task => task.Id),
            (TaskSortBy.DueDate, SortDirection.Desc) => query.OrderByDescending(task => task.DueDate).ThenByDescending(task => task.Id),
            (TaskSortBy.Priority, SortDirection.Asc) => query.OrderBy(task => task.Priority).ThenByDescending(task => task.Id),
            (TaskSortBy.Priority, SortDirection.Desc) => query.OrderByDescending(task => task.Priority).ThenByDescending(task => task.Id),
            (TaskSortBy.Status, SortDirection.Asc) => query.OrderBy(task => task.Status).ThenByDescending(task => task.Id),
            (TaskSortBy.Status, SortDirection.Desc) => query.OrderByDescending(task => task.Status).ThenByDescending(task => task.Id),
            (TaskSortBy.Title, SortDirection.Asc) => query.OrderBy(task => task.Title).ThenByDescending(task => task.Id),
            (TaskSortBy.Title, SortDirection.Desc) => query.OrderByDescending(task => task.Title).ThenByDescending(task => task.Id),
            (_, SortDirection.Asc) => query.OrderBy(task => task.CreatedAt).ThenByDescending(task => task.Id),
            _ => query.OrderByDescending(task => task.CreatedAt).ThenByDescending(task => task.Id)
        };
    }
}
