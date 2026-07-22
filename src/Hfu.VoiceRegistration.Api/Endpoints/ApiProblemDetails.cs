namespace Hfu.VoiceRegistration.Api.Endpoints;

public static class ApiProblemDetails
{
    public static IResult SessionNotFound(Guid sessionId)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Conversation session not found",
            detail: $"Conversation session '{sessionId}' was not found.");
    }
}
