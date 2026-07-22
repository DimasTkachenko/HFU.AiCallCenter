using Hfu.VoiceRegistration.Api.Contracts;
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
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.UpdateRegistrationFieldsAsync(
            sessionId,
            request.Fields,
            cancellationToken);

        return ToHttpResult(sessionId, result);
    }

    private static async Task<IResult> ConfirmRegistrationFieldsAsync(
        Guid sessionId,
        ConfirmRegistrationFieldsHttpRequest request,
        IRegistrationToolService registrationTools,
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.ConfirmRegistrationFieldsAsync(
            sessionId,
            request.FieldNames,
            cancellationToken);

        return ToHttpResult(sessionId, result);
    }

    private static async Task<IResult> MarkFieldsForClarificationAsync(
        Guid sessionId,
        MarkFieldsForClarificationHttpRequest request,
        IRegistrationToolService registrationTools,
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.MarkFieldsForClarificationAsync(
            sessionId,
            request.FieldNames,
            request.Reason,
            cancellationToken);

        return ToHttpResult(sessionId, result);
    }

    private static async Task<IResult> ClearRegistrationFieldsAsync(
        Guid sessionId,
        ClearRegistrationFieldsHttpRequest request,
        IRegistrationToolService registrationTools,
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.ClearRegistrationFieldsAsync(
            sessionId,
            request.FieldNames,
            cancellationToken);

        return ToHttpResult(sessionId, result);
    }

    private static async Task<IResult> GetRegistrationStateAsync(
        Guid sessionId,
        IRegistrationToolService registrationTools,
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.GetRegistrationStateAsync(
            sessionId,
            cancellationToken);

        return ToHttpResult(sessionId, result);
    }

    private static async Task<IResult> CompleteRegistrationAsync(
        Guid sessionId,
        CompleteRegistrationHttpRequest request,
        IRegistrationToolService registrationTools,
        CancellationToken cancellationToken)
    {
        var result = await registrationTools.CompleteRegistrationAsync(
            sessionId,
            request.ToApplicationRequest(),
            cancellationToken);

        return ToHttpResult(sessionId, result);
    }

    private static IResult ToHttpResult(
        Guid sessionId,
        RegistrationToolResult result)
    {
        return IsSessionNotFound(result)
            ? ApiProblemDetails.SessionNotFound(sessionId)
            : Results.Ok(result);
    }

    private static bool IsSessionNotFound(RegistrationToolResult result)
    {
        return result.State is null
            && result.Errors.Any(error => error.Code == RegistrationToolErrorCodes.SessionNotFound);
    }
}
