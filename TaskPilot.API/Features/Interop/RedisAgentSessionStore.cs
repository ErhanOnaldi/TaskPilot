using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.Caching.Distributed;

namespace TaskPilot.API.Features.Interop;

/// <summary>
/// Persists the small Agent Framework session envelope in Redis. The TaskPilot
/// Copilot conversation and messages remain the source of truth in PostgreSQL.
/// </summary>
internal sealed class RedisAgentSessionStore(IDistributedCache cache) : AgentSessionStore
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromDays(7)
    };

    public override async ValueTask SaveSessionAsync(
        AIAgent agent,
        string sessionStoreId,
        AgentSession session,
        CancellationToken cancellationToken)
    {
        var serialized = await agent.SerializeSessionAsync(session, cancellationToken: cancellationToken);
        await cache.SetStringAsync(
            CreateKey(agent, sessionStoreId),
            serialized.GetRawText(),
            CacheOptions,
            cancellationToken);
    }

    public override async ValueTask<AgentSession> GetSessionAsync(
        AIAgent agent,
        string sessionStoreId,
        CancellationToken cancellationToken)
    {
        var serialized = await cache.GetStringAsync(CreateKey(agent, sessionStoreId), cancellationToken);
        if (string.IsNullOrWhiteSpace(serialized))
            return await agent.CreateSessionAsync(cancellationToken);

        using var document = JsonDocument.Parse(serialized);
        return await agent.DeserializeSessionAsync(document.RootElement.Clone(), cancellationToken: cancellationToken);
    }

    public override async ValueTask DeleteSessionAsync(
        AIAgent agent,
        string sessionStoreId,
        CancellationToken cancellationToken) =>
        await cache.RemoveAsync(CreateKey(agent, sessionStoreId), cancellationToken);

    private static string CreateKey(AIAgent agent, string sessionStoreId)
    {
        var agentId = agent.Id ?? agent.Name ?? "agent";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes($"{agentId}\n{sessionStoreId}"));
        return $"taskpilot:a2a:session:{Convert.ToHexStringLower(digest)}";
    }
}
