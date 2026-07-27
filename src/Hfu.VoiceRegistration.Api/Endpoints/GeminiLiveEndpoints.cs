namespace Hfu.VoiceRegistration.Api.Endpoints;

using System.Net.WebSockets;
using Hfu.VoiceRegistration.Api.GeminiLive;

public static class GeminiLiveEndpoints
{
    public static RouteGroupBuilder MapGeminiLiveEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/conversation-sessions/{sessionId:guid}/gemini-live")
            .WithTags("Gemini Live");

        group.MapGet("/stream", StreamAudioAsync)
            .WithName("StreamGeminiLiveAudio");

        return group;
    }

    private static async Task StreamAudioAsync(
        Guid sessionId,
        HttpContext httpContext,
        GeminiLiveSessionHandler sessionHandler,
        CancellationToken cancellationToken)
    {
        if (!httpContext.WebSockets.IsWebSocketRequest)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsync("WebSocket request required.", cancellationToken);
            return;
        }

        using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
        await sessionHandler.HandleSessionAsync(sessionId, webSocket, cancellationToken);
    }
}
