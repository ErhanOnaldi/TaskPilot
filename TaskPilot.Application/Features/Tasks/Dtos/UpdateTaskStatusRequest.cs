using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Tasks.Dtos;

public sealed record UpdateTaskStatusRequest(TaskItemStatus Status);
