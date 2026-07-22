namespace Hfu.VoiceRegistration.Api.Endpoints;

using Hfu.VoiceRegistration.Domain.Conversations;

public static class ApiProblemDetails
{
    public static IResult SessionNotFound(Guid sessionId)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Conversation session not found",
            detail: $"Conversation session '{sessionId}' was not found.");
    }

    public static IResult RealtimeSdpOfferRequired(Guid sessionId)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Realtime SDP offer is required",
            detail: $"Conversation session '{sessionId}' requires a non-empty SDP offer to start a Realtime call.");
    }

    public static IResult RealtimeSdpMediaTypeRequired()
    {
        return Results.Problem(
            statusCode: StatusCodes.Status415UnsupportedMediaType,
            title: "Realtime SDP media type is required",
            detail: "Use Content-Type: application/sdp when starting a Realtime call.");
    }

    public static IResult RealtimeSdpOfferTooLarge(int maxCharacters)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status413PayloadTooLarge,
            title: "Realtime SDP offer is too large",
            detail: $"Realtime SDP offers must be {maxCharacters} characters or fewer.");
    }

    public static IResult RealtimeSessionCannotStart(
        Guid sessionId,
        ConversationSessionStatus status)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Conversation session cannot start realtime call",
            detail: $"Conversation session '{sessionId}' has status '{status}' and cannot start a Realtime call.");
    }

    public static IResult RealtimeConfigurationFailure(string detail)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Realtime configuration failure",
            detail: detail);
    }

    public static IResult OpenAIRealtimeRequestFailed(int openAIStatusCode)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "OpenAI Realtime request failed",
            detail: $"OpenAI Realtime returned HTTP {openAIStatusCode}.");
    }

    public static IResult OpenAIRealtimeTransportFailed()
    {
        return Results.Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "OpenAI Realtime request failed",
            detail: "OpenAI Realtime could not be reached.");
    }
}
