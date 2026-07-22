using System.Text;
using Hfu.VoiceRegistration.Api.OpenAIRealtime;
using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Domain.Conversations;
using Microsoft.Extensions.Options;

namespace Hfu.VoiceRegistration.Api.Endpoints;

public static class OpenAIRealtimeEndpoints
{
    public const string RateLimitPolicyName = "openai-realtime-calls";

    public static RouteGroupBuilder MapOpenAIRealtimeEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/conversation-sessions/{sessionId:guid}/realtime")
            .WithTags("OpenAI Realtime");

        group.MapPost("/calls", CreateCallAsync)
            .WithName("CreateOpenAIRealtimeCall")
            .RequireRateLimiting(RateLimitPolicyName)
            .Produces(StatusCodes.Status200OK, contentType: "application/sdp")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return group;
    }

    private static async Task<IResult> CreateCallAsync(
        Guid sessionId,
        HttpRequest request,
        IConversationSessionStore store,
        IOpenAIRealtimeClient openAIRealtimeClient,
        IOptions<OpenAIRealtimeOptions> optionsAccessor,
        CancellationToken cancellationToken)
    {
        var session = await store.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return ApiProblemDetails.SessionNotFound(sessionId);
        }

        if (session.Status is ConversationSessionStatus.Completed or ConversationSessionStatus.Abandoned)
        {
            return ApiProblemDetails.RealtimeSessionCannotStart(sessionId, session.Status);
        }

        if (!IsSdpContentType(request.ContentType))
        {
            return ApiProblemDetails.RealtimeSdpMediaTypeRequired();
        }

        var maxSdpOfferCharacters = optionsAccessor.Value.EffectiveRealtimeMaxSdpOfferCharacters;
        if (request.ContentLength > maxSdpOfferCharacters)
        {
            return ApiProblemDetails.RealtimeSdpOfferTooLarge(maxSdpOfferCharacters);
        }

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false);
        var sdpOffer = await reader.ReadToEndAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sdpOffer))
        {
            return ApiProblemDetails.RealtimeSdpOfferRequired(sessionId);
        }

        if (sdpOffer.Length > maxSdpOfferCharacters)
        {
            return ApiProblemDetails.RealtimeSdpOfferTooLarge(maxSdpOfferCharacters);
        }

        try
        {
            var result = await openAIRealtimeClient.CreateCallAsync(
                sdpOffer,
                $"hfu-session-{sessionId:N}",
                cancellationToken);

            return Results.Text(result.SdpAnswer, "application/sdp", Encoding.UTF8);
        }
        catch (OpenAIRealtimeConfigurationException exception)
        {
            return ApiProblemDetails.RealtimeConfigurationFailure(exception.Message);
        }
        catch (OpenAIRealtimeApiException exception)
        {
            return ApiProblemDetails.OpenAIRealtimeRequestFailed(exception.StatusCode);
        }
        catch (HttpRequestException)
        {
            return ApiProblemDetails.OpenAIRealtimeTransportFailed();
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiProblemDetails.OpenAIRealtimeTransportFailed();
        }
    }

    private static bool IsSdpContentType(string? contentType)
    {
        return contentType?.StartsWith("application/sdp", StringComparison.OrdinalIgnoreCase) == true;
    }
}
