using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.WorkspaceMembers.Dtos;

/// <summary>
/// Invites an existing user to a workspace. Email is the public contract; UserId is retained
/// temporarily so existing clients can migrate without a breaking change.
/// </summary>
public sealed record AddWorkspaceMemberRequest
{
    public AddWorkspaceMemberRequest()
    {
    }

    public AddWorkspaceMemberRequest(string email, Role role = Role.Member)
    {
        Email = email;
        Role = role;
    }

    [Obsolete("Use the email-based constructor or Email property instead.")]
    public AddWorkspaceMemberRequest(int userId, Role role = Role.Member)
    {
        UserId = userId;
        Role = role;
    }

    public string? Email { get; init; }

    [Obsolete("Use Email for workspace invitations.")]
    public int? UserId { get; init; }

    public Role Role { get; init; } = Role.Member;
}
