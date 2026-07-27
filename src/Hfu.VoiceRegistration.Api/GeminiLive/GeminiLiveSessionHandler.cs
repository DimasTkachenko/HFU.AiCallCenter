namespace Hfu.VoiceRegistration.Api.GeminiLive;

using System.Net.WebSockets;
using Hfu.VoiceRegistration.Api.OpenAIRealtime;
using Hfu.VoiceRegistration.Api.Realtime;
using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Application.RegistrationTools;
using Hfu.VoiceRegistration.Infrastructure.GeminiLive.Protocol;
using Microsoft.Extensions.Options;

public sealed class GeminiLiveSessionHandler
{
    private readonly IGeminiLiveClient _geminiClient;
    private readonly IConversationSessionStore _sessionStore;
    private readonly IRegistrationToolService _registrationTools;
    private readonly IConversationRealtimeNotifier _realtimeNotifier;
    private readonly GeminiLiveOptions _options;

    public GeminiLiveSessionHandler(
        IGeminiLiveClient geminiClient,
        IConversationSessionStore sessionStore,
        IRegistrationToolService registrationTools,
        IConversationRealtimeNotifier realtimeNotifier,
        IOptions<GeminiLiveOptions> options)
    {
        _geminiClient = geminiClient;
        _sessionStore = sessionStore;
        _registrationTools = registrationTools;
        _realtimeNotifier = realtimeNotifier;
        _options = options.Value;
    }

    public async Task HandleSessionAsync(
        Guid sessionId,
        WebSocket clientWebSocket,
        CancellationToken cancellationToken)
    {
        var session = await _sessionStore.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            await clientWebSocket.CloseAsync(
                WebSocketCloseStatus.PolicyViolation,
                "Session not found",
                cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            await clientWebSocket.CloseAsync(
                WebSocketCloseStatus.PolicyViolation,
                "Gemini Live API Key is not configured. Please set GeminiLive:ApiKey in appsettings or user secrets.",
                cancellationToken);
            return;
        }

        var systemPrompt = OpenAIRealtimeRegistrationPrompt.CurrentInstructions;
        var tools = new object[] { GeminiLiveToolsBuilder.BuildRegistrationTools() };
        var inputAudioEnabled = false;

        _geminiClient.OnAudioReceived += async (pcm24kAudio) =>
        {
            if (clientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    var frame = new byte[1 + pcm24kAudio.Length];
                    frame[0] = 0x01; // Audio payload marker
                    Buffer.BlockCopy(pcm24kAudio, 0, frame, 1, pcm24kAudio.Length);
                    await clientWebSocket.SendAsync(
                        new ArraySegment<byte>(frame),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None);
                }
                catch { }
            }
        };

        _geminiClient.OnInputAudioEnabled += async () =>
        {
            inputAudioEnabled = true;
            if (clientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    var frame = new byte[] { 0x03 }; // Input audio enabled marker
                    await clientWebSocket.SendAsync(
                        new ArraySegment<byte>(frame),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None);
                }
                catch { }
            }
        };

        _geminiClient.OnInputAudioDisabled += async () =>
        {
            inputAudioEnabled = false;
            if (clientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    var frame = new byte[] { 0x04 }; // Input audio disabled marker
                    await clientWebSocket.SendAsync(
                        new ArraySegment<byte>(frame),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None);
                }
                catch { }
            }
        };

        _geminiClient.OnInterrupted += async () =>
        {
            if (clientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    var frame = new byte[] { 0x02 }; // Interrupted marker
                    await clientWebSocket.SendAsync(
                        new ArraySegment<byte>(frame),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None);
                }
                catch { }
            }
        };

        _geminiClient.OnFunctionCallReceived += async (functionCall) =>
        {
            await ExecuteFunctionCallAsync(sessionId, functionCall, cancellationToken);
        };

        try
        {
            await _geminiClient.ConnectAsync(
                systemPrompt,
                tools,
                _options.EffectiveModel,
                _options.EffectiveVoiceName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            if (clientWebSocket.State == WebSocketState.Open)
            {
                await clientWebSocket.CloseAsync(
                    WebSocketCloseStatus.InternalServerError,
                    $"Failed to connect to Gemini Live service: {ex.Message}",
                    cancellationToken);
            }
            return;
        }

        var buffer = new byte[16 * 1024];
        try
        {
            while (clientWebSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await clientWebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                {
                    if (!inputAudioEnabled)
                    {
                        continue;
                    }

                    var pcm16kChunk = new byte[result.Count];
                    Buffer.BlockCopy(buffer, 0, pcm16kChunk, 0, result.Count);
                    await _geminiClient.SendAudioChunkAsync(pcm16kChunk, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            await _geminiClient.DisconnectAsync();
            if (clientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    await clientWebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Session ended",
                        CancellationToken.None);
                }
                catch { }
            }
        }
    }

    private async Task ExecuteFunctionCallAsync(
        Guid sessionId,
        GeminiFunctionCall functionCall,
        CancellationToken cancellationToken)
    {
        object responseResult;

        try
        {
            switch (functionCall.Name)
            {
                case "update_registration_fields":
                    responseResult = await HandleUpdateFieldsAsync(sessionId, functionCall.Args, cancellationToken);
                    break;
                case "confirm_registration_fields":
                    responseResult = await HandleConfirmFieldsAsync(sessionId, functionCall.Args, cancellationToken);
                    break;
                case "mark_fields_for_clarification":
                    responseResult = await HandleMarkFieldsAsync(sessionId, functionCall.Args, cancellationToken);
                    break;
                case "clear_registration_fields":
                    responseResult = await HandleClearFieldsAsync(sessionId, functionCall.Args, cancellationToken);
                    break;
                case "get_registration_state":
                    var stateResult = await _registrationTools.GetRegistrationStateAsync(sessionId, cancellationToken);
                    responseResult = stateResult.State != null ? (object)stateResult.State : new { succeeded = stateResult.Succeeded, errors = stateResult.Errors };
                    break;
                case "complete_registration":
                    responseResult = await HandleCompleteRegistrationAsync(sessionId, functionCall.Args, cancellationToken);
                    break;
                default:
                    responseResult = new { succeeded = false, error = $"Unknown function {functionCall.Name}" };
                    break;
            }
        }
        catch (Exception ex)
        {
            responseResult = new { succeeded = false, error = ex.Message };
        }

        await _geminiClient.SendToolResponseAsync(
            functionCall.Id,
            functionCall.Name,
            responseResult,
            cancellationToken);
    }

    private async Task<object> HandleUpdateFieldsAsync(
        Guid sessionId,
        System.Text.Json.Nodes.JsonObject? args,
        CancellationToken cancellationToken)
    {
        if (args == null || !args.TryGetPropertyValue("fields", out var fieldsNode) || fieldsNode == null)
        {
            return new { succeeded = false, error = "Missing fields argument" };
        }

        var updates = new List<RegistrationFieldUpdate>();
        foreach (var item in fieldsNode.AsArray())
        {
            if (item is System.Text.Json.Nodes.JsonObject obj &&
                obj.TryGetPropertyValue("name", out var nameNode) &&
                obj.TryGetPropertyValue("value", out var valNode))
            {
                var name = nameNode?.ToString() ?? "";
                var val = valNode?.ToString() ?? "";
                var raw = obj.TryGetPropertyValue("rawValue", out var rawNode) ? rawNode?.ToString() : null;
                updates.Add(new RegistrationFieldUpdate(name, val, raw));
            }
        }

        var result = await _registrationTools.UpdateRegistrationFieldsAsync(sessionId, updates, cancellationToken);
        if (result.Succeeded)
        {
            await _realtimeNotifier.NotifyAsync(
                sessionId,
                result.State?.Version ?? 0L,
                ConversationRealtimeEventType.RegistrationStateChanged,
                "Registration state changed.",
                cancellationToken);
            return result.State!;
        }
        return new { succeeded = false, errors = result.Errors };
    }

    private async Task<object> HandleConfirmFieldsAsync(
        Guid sessionId,
        System.Text.Json.Nodes.JsonObject? args,
        CancellationToken cancellationToken)
    {
        var names = GetFieldNames(args);
        var result = await _registrationTools.ConfirmRegistrationFieldsAsync(sessionId, names, cancellationToken);
        if (result.Succeeded)
        {
            await _realtimeNotifier.NotifyAsync(
                sessionId,
                result.State?.Version ?? 0L,
                ConversationRealtimeEventType.RegistrationStateChanged,
                "Registration fields confirmed.",
                cancellationToken);
            return result.State!;
        }
        return new { succeeded = false, errors = result.Errors };
    }

    private async Task<object> HandleMarkFieldsAsync(
        Guid sessionId,
        System.Text.Json.Nodes.JsonObject? args,
        CancellationToken cancellationToken)
    {
        var names = GetFieldNames(args);
        var reason = args?.TryGetPropertyValue("reason", out var rNode) == true ? rNode?.ToString() : null;
        var result = await _registrationTools.MarkFieldsForClarificationAsync(sessionId, names, reason, cancellationToken);
        if (result.Succeeded)
        {
            await _realtimeNotifier.NotifyAsync(
                sessionId,
                result.State?.Version ?? 0L,
                ConversationRealtimeEventType.RegistrationStateChanged,
                "Registration fields require clarification.",
                cancellationToken);
            return result.State!;
        }
        return new { succeeded = false, errors = result.Errors };
    }

    private async Task<object> HandleClearFieldsAsync(
        Guid sessionId,
        System.Text.Json.Nodes.JsonObject? args,
        CancellationToken cancellationToken)
    {
        var names = GetFieldNames(args);
        var result = await _registrationTools.ClearRegistrationFieldsAsync(sessionId, names, cancellationToken);
        if (result.Succeeded)
        {
            await _realtimeNotifier.NotifyAsync(
                sessionId,
                result.State?.Version ?? 0L,
                ConversationRealtimeEventType.RegistrationStateChanged,
                "Registration fields cleared.",
                cancellationToken);
            return result.State!;
        }
        return new { succeeded = false, errors = result.Errors };
    }

    private async Task<object> HandleCompleteRegistrationAsync(
        Guid sessionId,
        System.Text.Json.Nodes.JsonObject? args,
        CancellationToken cancellationToken)
    {
        var consent = args?.TryGetPropertyValue("personalDataConsent", out var cNode) == true && (cNode?.GetValue<bool>() ?? false);
        var confirmed = args?.TryGetPropertyValue("registrationConfirmed", out var rNode) == true && (rNode?.GetValue<bool>() ?? false);

        var request = new CompleteRegistrationRequest(consent, confirmed);
        var result = await _registrationTools.CompleteRegistrationAsync(sessionId, request, cancellationToken);
        if (result.Succeeded)
        {
            await _realtimeNotifier.NotifyAsync(
                sessionId,
                result.State?.Version ?? 0L,
                ConversationRealtimeEventType.RegistrationStateChanged,
                "Registration completed successfully.",
                cancellationToken);
            return result.State!;
        }
        return new { succeeded = false, errors = result.Errors };
    }

    private static List<string> GetFieldNames(System.Text.Json.Nodes.JsonObject? args)
    {
        var list = new List<string>();
        if (args != null && args.TryGetPropertyValue("fieldNames", out var arrNode) && arrNode != null)
        {
            foreach (var item in arrNode.AsArray())
            {
                if (item != null) list.Add(item.ToString());
            }
        }
        return list;
    }
}
