using Microsoft.AspNetCore.Authorization;
using TaskPilot.Application.Authorization.Enums;

namespace TaskPilot.Infrastructure.Authorization.Requirements;

public sealed record WorkspaceAccessRequirement(WorkspaceAccessLevel AccessLevel) : IAuthorizationRequirement;
