using Hfu.VoiceRegistration.Api.Contracts;
using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Application.RegistrationTools;
using Hfu.VoiceRegistration.Domain.Conversations;

namespace Hfu.VoiceRegistration.Api.Endpoints;

public static class ConversationSessionEndpoints
{
    private const string SessionAbandonedEventType = "SessionAbandoned";

    public static RouteGroupBuilder MapConversationSessionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/conversation-sessions")
            .WithTags("Conversation Sessions");

        group.MapPost("/", CreateSessionAsync)
            .WithName("CreateConversationSession");

        group.MapGet("/{sessionId:guid}", GetSessionAsync)
            .WithName("GetConversationSession");

        group.MapPost("/{sessionId:guid}/abandon", AbandonSessionAsync)
            .WithName("AbandonConversationSession");

        return group;
    }

    private static async Task<IResult> CreateSessionAsync(
        IConversationSessionStore store,
        IRegistrationToolService registrationTools,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var session = ConversationSession.Create(now);

        await store.CreateAsync(session, cancellationToken);
        var toolResult = await registrationTools.GetRegistrationStateAsync(
            session.SessionId,
            cancellationToken);

        var response = ApiContractMapper.ToResponse(session, toolResult.State!);

        return Results.Created(
            $"/api/conversation-sessions/{session.SessionId}",
            response);
    }

    private static async Task<IResult> GetSessionAsync(
        Guid sessionId,
        IConversationSessionStore store,
        IRegistrationToolService registrationTools,
        CancellationToken cancellationToken)
    {
        var session = await store.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return ApiProblemDetails.SessionNotFound(sessionId);
        }

        var toolResult = await registrationTools.GetRegistrationStateAsync(
            sessionId,
            cancellationToken);

        return Results.Ok(ApiContractMapper.ToResponse(session, toolResult.State!));
    }

    private static async Task<IResult> AbandonSessionAsync(
        Guid sessionId,
        IConversationSessionStore store,
        IRegistrationToolService registrationTools,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var existing = await store.GetAsync(sessionId, cancellationToken);
        if (existing is null)
        {
            return ApiProblemDetails.SessionNotFound(sessionId);
        }

        if (existing.Status == ConversationSessionStatus.Completed)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conversation session is completed",
                detail: $"Conversation session '{sessionId}' is already completed and cannot be abandoned.");
        }

        var updated = await store.UpdateAsync(
            sessionId,
            (current, _) =>
            {
                var now = timeProvider.GetUtcNow();
                var abandoned = (current with
                {
                    Status = ConversationSessionStatus.Abandoned,
                    LastActivityAt = now
                }).RecordEvent(
                    SessionAbandonedEventType,
                    "Conversation session was abandoned.",
                    now);

                return Task.FromResult(abandoned);
            },
            cancellationToken);

        var toolResult = await registrationTools.GetRegistrationStateAsync(
            sessionId,
            cancellationToken);

        return Results.Ok(ApiContractMapper.ToResponse(updated, toolResult.State!));
    }
}
