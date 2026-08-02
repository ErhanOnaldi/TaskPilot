namespace TaskPilot.Application.Features.Knowledge.Authorization;

public sealed record KnowledgeAccessResult(int CurrentUserId, ServiceResult? Failure)
{
    public static KnowledgeAccessResult Denied(ServiceResult failure, int currentUserId) => new(currentUserId, failure);
}
