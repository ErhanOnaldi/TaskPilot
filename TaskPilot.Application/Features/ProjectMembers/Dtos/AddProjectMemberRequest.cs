using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.ProjectMembers.Dtos;

public sealed record AddProjectMemberRequest(int UserId, ProjectRole Role);
