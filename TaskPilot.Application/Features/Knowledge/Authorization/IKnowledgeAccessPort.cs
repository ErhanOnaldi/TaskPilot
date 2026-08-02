namespace TaskPilot.Application.Features.Knowledge.Authorization;

/// <summary>Application-owned access port: Guest=Read, Member+=Edit, Manager/Owner=Manage.</summary>
public interface IKnowledgeAccessPort
{
    Task<KnowledgeAccessResult> AuthorizeAsync(
        int workspaceId,
        KnowledgeAccessLevel accessLevel,
        CancellationToken cancellationToken);

    Task<KnowledgeAccessResult> AuthorizeAsync(
        int workspaceId,
        int? projectId,
        KnowledgeAccessLevel accessLevel,
        CancellationToken cancellationToken) =>
        AuthorizeAsync(workspaceId, accessLevel, cancellationToken);
}
