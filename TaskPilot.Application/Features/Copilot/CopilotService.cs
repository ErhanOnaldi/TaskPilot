using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.AI.Copilot;

namespace TaskPilot.Application.Features.Copilot;

public sealed class CopilotService(
    ICopilotRepository repository,
    ICopilotAuthorizationPort authorization,
    ICopilotContextRetriever retrieval,
    IAiChatGenerator generator,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : ICopilotService
{
    private const int MaxUserMessageCharacters = 4_000;
    private const int MaxPromptMessages = 12;

    public Task<ServiceResult<CopilotSessionResponse>> ChatProjectAsync(
        int projectId,
        CopilotRequest request,
        string? correlationId,
        CancellationToken cancellationToken) =>
        ChatAsync(
            ct => authorization.AuthorizeProjectAsync(projectId, correlationId, request.CausationId, ct),
            request,
            cancellationToken);

    public Task<ServiceResult<CopilotSessionResponse>> ChatWorkspaceAsync(
        int workspaceId,
        CopilotRequest request,
        string? correlationId,
        CancellationToken cancellationToken) =>
        ChatAsync(
            ct => authorization.AuthorizeWorkspaceAsync(workspaceId, correlationId, request.CausationId, ct),
            request,
            cancellationToken);

    public Task<ServiceResult<CopilotStreamResponse>> StreamProjectAsync(
        int projectId,
        CopilotRequest request,
        string? correlationId,
        CancellationToken cancellationToken) =>
        StartStreamAsync(
            ct => authorization.AuthorizeProjectAsync(projectId, correlationId, request.CausationId, ct),
            request,
            cancellationToken);

    public Task<ServiceResult<CopilotStreamResponse>> StreamWorkspaceAsync(
        int workspaceId,
        CopilotRequest request,
        string? correlationId,
        CancellationToken cancellationToken) =>
        StartStreamAsync(
            ct => authorization.AuthorizeWorkspaceAsync(workspaceId, correlationId, request.CausationId, ct),
            request,
            cancellationToken);

    public async Task<ServiceResult<CopilotSessionResponse>> GetSessionAsync(int sessionId, CancellationToken cancellationToken)
    {
        var session = await repository.GetSessionAsync(sessionId, cancellationToken);
        if (session is null)
            return ServiceResult<CopilotSessionResponse>.Fail("Copilot session not found.", HttpStatusCode.NotFound);

        var scope = session.ProjectId is { } projectId
            ? await authorization.AuthorizeProjectAsync(projectId, null, null, cancellationToken)
            : await authorization.AuthorizeWorkspaceAsync(session.WorkspaceId, null, null, cancellationToken);
        if (scope is null || scope.ExecutionScope.UserId != session.UserId)
            return ServiceResult<CopilotSessionResponse>.Fail("Copilot session access denied.", HttpStatusCode.Forbidden);

        return ServiceResult<CopilotSessionResponse>.Success(ToResponse(session));
    }

    private async Task<ServiceResult<CopilotSessionResponse>> ChatAsync(
        Func<CancellationToken, Task<CopilotAuthorizedScope?>> authorize,
        CopilotRequest request,
        CancellationToken cancellationToken)
    {
        var preparation = await PrepareAsync(authorize, request, cancellationToken);
        if (preparation.Error is not null)
            return ServiceResult<CopilotSessionResponse>.Fail(preparation.Error, preparation.Status);

        var chat = preparation.Chat!;
        var answer = await generator.CompleteAsync(chat.Prompt, chat.Scope.ExecutionScope, cancellationToken);
        if (string.IsNullOrWhiteSpace(answer))
            throw new InvalidOperationException("AI generation failed.");

        var completion = CompleteCitations(answer, chat.Context);
        var response = await PersistCompletionAsync(chat.Session, completion.Answer, completion.Citations, cancellationToken);
        return ServiceResult<CopilotSessionResponse>.Success(response);
    }

    private async Task<ServiceResult<CopilotStreamResponse>> StartStreamAsync(
        Func<CancellationToken, Task<CopilotAuthorizedScope?>> authorize,
        CopilotRequest request,
        CancellationToken cancellationToken)
    {
        var preparation = await PrepareAsync(authorize, request, cancellationToken);
        if (preparation.Error is not null)
            return ServiceResult<CopilotStreamResponse>.Fail(preparation.Error, preparation.Status);

        var chat = preparation.Chat!;
        return ServiceResult<CopilotStreamResponse>.Success(
            new CopilotStreamResponse(chat.Session.Id, StreamUpdatesAsync(chat, cancellationToken)));
    }

    private async IAsyncEnumerable<CopilotStreamUpdate> StreamUpdatesAsync(
        PreparedChat chat,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var answer = new StringBuilder();
        await foreach (var token in generator.StreamAsync(chat.Prompt, chat.Scope.ExecutionScope, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (string.IsNullOrEmpty(token))
                continue;
            answer.Append(token);
            yield return new CopilotStreamUpdate(CopilotStreamUpdateKind.Token, token);
        }

        if (string.IsNullOrWhiteSpace(answer.ToString()))
            throw new InvalidOperationException("AI generation failed.");

        var completion = CompleteCitations(answer.ToString(), chat.Context);
        if (completion.Answer.Length > answer.Length)
        {
            var citationToken = completion.Answer[answer.Length..];
            yield return new CopilotStreamUpdate(CopilotStreamUpdateKind.Token, citationToken);
        }

        var response = await PersistCompletionAsync(
            chat.Session,
            completion.Answer,
            completion.Citations,
            cancellationToken);
        yield return new CopilotStreamUpdate(CopilotStreamUpdateKind.Completed, Session: response);
    }

    private async Task<PreparationResult> PrepareAsync(
        Func<CancellationToken, Task<CopilotAuthorizedScope?>> authorize,
        CopilotRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > MaxUserMessageCharacters)
            return PreparationResult.Failure("Copilot message must be between 1 and 4000 characters.", HttpStatusCode.BadRequest);

        var scope = await authorize(cancellationToken);
        if (scope is null)
            return PreparationResult.Failure("Copilot access denied.", HttpStatusCode.Forbidden);

        var session = request.SessionId is { } id
            ? await repository.GetSessionAsync(id, cancellationToken)
            : null;
        if (request.SessionId is not null &&
            (session is null ||
             session.UserId != scope.ExecutionScope.UserId ||
             session.WorkspaceId != scope.ExecutionScope.WorkspaceId ||
             session.ProjectId != scope.ExecutionScope.ProjectId))
            return PreparationResult.Failure("Copilot session access denied.", HttpStatusCode.Forbidden);

        if (session is null)
        {
            session = new CopilotChatSession
            {
                WorkspaceId = scope.ExecutionScope.WorkspaceId,
                ProjectId = scope.ExecutionScope.ProjectId,
                UserId = scope.ExecutionScope.UserId,
                CreatedAtUtc = clock.UtcNow,
                UpdatedAtUtc = clock.UtcNow
            };
            await repository.AddSessionAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var userMessage = new CopilotChatMessage
        {
            SessionId = session.Id,
            Role = CopilotMessageRole.User,
            Content = request.Message.Trim(),
            CreatedAtUtc = clock.UtcNow
        };
        await repository.AddMessageAsync(userMessage, cancellationToken);
        session.Messages.Add(userMessage);

        var context = await retrieval.RetrieveAsync(scope, userMessage.Content, cancellationToken);
        if (context.Count == 0)
            return PreparationResult.Failure(
                "No authorized task or note context is available for citation.",
                HttpStatusCode.Conflict);

        var prompt = Compact(session.Messages)
            .Prepend(new AiChatMessage(
                "system",
                "Answer only from authorized context. Cite every factual claim with [Task:<id>] or [Note:<id>]."))
            .Concat(context.Select(item => new AiChatMessage(
                "system",
                $"Authorized {item.Citation.Type} {item.Citation.SourceId}: {item.Content}")))
            .ToList();

        return PreparationResult.Success(new PreparedChat(scope, session, context, prompt));
    }

    private async Task<CopilotSessionResponse> PersistCompletionAsync(
        CopilotChatSession session,
        string answer,
        IReadOnlyList<CopilotCitation> citations,
        CancellationToken cancellationToken)
    {
        var assistantMessage = new CopilotChatMessage
        {
            SessionId = session.Id,
            Role = CopilotMessageRole.Assistant,
            Content = answer,
            CitationsJson = JsonSerializer.Serialize(citations),
            CreatedAtUtc = clock.UtcNow
        };
        await repository.AddMessageAsync(assistantMessage, cancellationToken);
        session.Messages.Add(assistantMessage);
        session.UpdatedAtUtc = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(session);
    }

    private static Completion CompleteCitations(string answer, IReadOnlyList<CopilotContextItem> context)
    {
        var citations = context
            .Select(item => item.Citation)
            .Distinct()
            .Where(citation => ContainsCitation(answer, citation))
            .ToList();
        if (citations.Count > 0)
            return new Completion(answer, citations);

        var fallback = context[0].Citation;
        return new Completion(
            $"{answer.Trim()}\n\nSource: [{fallback.Type}:{fallback.SourceId}]",
            [fallback]);
    }

    public static IReadOnlyList<AiChatMessage> Compact(IEnumerable<CopilotChatMessage> messages)
    {
        var ordered = messages.OrderBy(message => message.CreatedAtUtc).ToList();
        if (ordered.Count <= MaxPromptMessages)
            return ordered
                .Select(message => new AiChatMessage(message.Role.ToString().ToLowerInvariant(), message.Content))
                .ToList();

        var summary = string.Join(
            " | ",
            ordered.Take(ordered.Count - MaxPromptMessages + 1).Select(message => $"{message.Role}: {message.Content}"));
        return new[]
            {
                new AiChatMessage(
                    "system",
                    "Conversation summary (deterministic): " + summary[..Math.Min(1_500, summary.Length)])
            }
            .Concat(ordered.TakeLast(MaxPromptMessages - 1).Select(message =>
                new AiChatMessage(message.Role.ToString().ToLowerInvariant(), message.Content)))
            .ToList();
    }

    private static CopilotSessionResponse ToResponse(CopilotChatSession session) =>
        new(
            session.Id,
            session.WorkspaceId,
            session.ProjectId,
            session.Messages
                .OrderBy(message => message.CreatedAtUtc)
                .Select(message => new CopilotMessageResponse(
                    message.Id,
                    message.Role.ToString(),
                    message.Content,
                    DeserializeCitations(message.CitationsJson),
                    message.CreatedAtUtc))
                .ToList());

    private static bool ContainsCitation(string answer, CopilotCitation citation) =>
        answer.Contains($"[{citation.Type}:{citation.SourceId}]", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<CopilotCitation> DeserializeCitations(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<List<CopilotCitation>>(value) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record PreparedChat(
        CopilotAuthorizedScope Scope,
        CopilotChatSession Session,
        IReadOnlyList<CopilotContextItem> Context,
        IReadOnlyList<AiChatMessage> Prompt);

    private sealed record PreparationResult(PreparedChat? Chat, string? Error, HttpStatusCode Status)
    {
        public static PreparationResult Success(PreparedChat chat) => new(chat, null, HttpStatusCode.OK);
        public static PreparationResult Failure(string error, HttpStatusCode status) => new(null, error, status);
    }

    private sealed record Completion(string Answer, IReadOnlyList<CopilotCitation> Citations);
}

public static class CopilotApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCopilotApplication(this IServiceCollection services)
    {
        services.AddScoped<ICopilotService, CopilotService>();
        services.AddScoped<ICopilotContextRetriever, SemanticCopilotContextRetriever>();
        return services;
    }
}
