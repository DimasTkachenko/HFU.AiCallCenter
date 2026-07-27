using Hfu.VoiceRegistration.Api.Contracts;
using Hfu.VoiceRegistration.Api.Realtime;
using Hfu.VoiceRegistration.Application.RegistrationTools;

namespace Hfu.VoiceRegistration.Api.Endpoints;

public static class RegistrationToolEndpoints
{
    public static RouteGroupBuilder MapRegistrationToolEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/conversation-sessions/{sessionId:guid}/tools")
            .WithTags("Registration Tools");

        group.MapPost("/update-registration-fields", UpdateRegistrationFieldsAsync)
            .WithName("UpdateRegistrationFields");

        group.MapPost("/confirm-registration-fields", ConfirmRegistrationFieldsAsync)
            .WithName("ConfirmRegistrationFields");

        group.MapPost("/mark-fields-for-clarification", MarkFieldsForClarificationAsync)
            .WithName("MarkFieldsForClarification");

        group.MapPost("/clear-registration-fields", ClearRegistrationFieldsAsync)
            .WithName("ClearRegistrationFields");

        group.MapPost("/get-registration-state", GetRegistrationStateAsync)
            .WithName("GetRegistrationState");

        group.MapPost("/complete-registration", CompleteRegistrationAsync)
            .WithName("CompleteRegistration");

        return group;
    }

    private static async Task<IResult> UpdateRegistrationFieldsAsync(
        Guid sessionId,
        UpdateRegistrationFieldsHttpRequest request,
        IRegistrationToolService registrationTools,
        IConversationRealtimeNotifier realtimeNotifier,
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.UpdateRegistrationFieldsAsync(
            sessionId,
            request.Fields,
            cancellationToken);

        return await ToHttpResultAsync(
            sessionId,
            result,
            realtimeNotifier,
            ConversationRealtimeEventType.RegistrationStateChanged,
            "Registration state changed.",
            cancellationToken);
    }

    private static async Task<IResult> ConfirmRegistrationFieldsAsync(
        Guid sessionId,
        ConfirmRegistrationFieldsHttpRequest request,
        IRegistrationToolService registrationTools,
        IConversationRealtimeNotifier realtimeNotifier,
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.ConfirmRegistrationFieldsAsync(
            sessionId,
            request.FieldNames,
            cancellationToken);

        return await ToHttpResultAsync(
            sessionId,
            result,
            realtimeNotifier,
            ConversationRealtimeEventType.RegistrationStateChanged,
            "Registration fields confirmed.",
            cancellationToken);
    }

    private static async Task<IResult> MarkFieldsForClarificationAsync(
        Guid sessionId,
        MarkFieldsForClarificationHttpRequest request,
        IRegistrationToolService registrationTools,
        IConversationRealtimeNotifier realtimeNotifier,
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.MarkFieldsForClarificationAsync(
            sessionId,
            request.FieldNames,
            request.Reason,
            cancellationToken);

        return await ToHttpResultAsync(
            sessionId,
            result,
            realtimeNotifier,
            ConversationRealtimeEventType.RegistrationStateChanged,
            "Registration fields require clarification.",
            cancellationToken);
    }

    private static async Task<IResult> ClearRegistrationFieldsAsync(
        Guid sessionId,
        ClearRegistrationFieldsHttpRequest request,
        IRegistrationToolService registrationTools,
        IConversationRealtimeNotifier realtimeNotifier,
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.ClearRegistrationFieldsAsync(
            sessionId,
            request.FieldNames,
            cancellationToken);

        return await ToHttpResultAsync(
            sessionId,
            result,
            realtimeNotifier,
            ConversationRealtimeEventType.RegistrationStateChanged,
            "Registration fields cleared.",
            cancellationToken);
    }

    private static async Task<IResult> GetRegistrationStateAsync(
        Guid sessionId,
        IRegistrationToolService registrationTools,
        IConversationRealtimeNotifier realtimeNotifier,
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.GetRegistrationStateAsync(
            sessionId,
            cancellationToken);

        return await ToHttpResultAsync(
            sessionId,
            result,
            realtimeNotifier,
            ConversationRealtimeEventType.DiagnosticEventAdded,
            "Registration state requested.",
            cancellationToken);
    }

    private static async Task<IResult> CompleteRegistrationAsync(
        Guid sessionId,
        CompleteRegistrationHttpRequest request,
        IRegistrationToolService registrationTools,
        IConversationRealtimeNotifier realtimeNotifier,
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.CompleteRegistrationAsync(
            sessionId,
            request.ToApplicationRequest(),
            cancellationToken);

        return await ToHttpResultAsync(
            sessionId,
            result,
            realtimeNotifier,
            result.Completion is null
                ? ConversationRealtimeEventType.RegistrationToolCompleted
                : ConversationRealtimeEventType.RegistrationCompleted,
            result.Completion is null
                ? "Registration completion requested."
                : "Registration completed.",
            cancellationToken);
    }

    private static async Task<IResult> ToHttpResultAsync(
        Guid sessionId,
        RegistrationToolResult result,
        IConversationRealtimeNotifier realtimeNotifier,
        ConversationRealtimeEventType successType,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (IsSessionNotFound(result))
        {
            return ApiProblemDetails.SessionNotFound(sessionId);
        }

        if (result.State is not null)
        {
            var eventType = result.Errors.Count == 0
                ? successType
                : ConversationRealtimeEventType.ValidationFailed;
            var message = result.Errors.Count == 0
                ? successMessage
                : "Registration validation failed.";

            await realtimeNotifier.NotifyAsync(
                sessionId,
                result.State.Version,
                eventType,
                message,
                cancellationToken);
        }

        return Results.Ok(result);
    }

    private static bool IsSessionNotFound(RegistrationToolResult result)
    {
        return result.State is null
            && result.Errors.Any(error => error.Code == RegistrationToolErrorCodes.SessionNotFound);
    }
}
