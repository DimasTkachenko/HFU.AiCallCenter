namespace Hfu.VoiceRegistration.Infrastructure.GeminiLive;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Hfu.VoiceRegistration.Api.GeminiLive;
using Hfu.VoiceRegistration.Infrastructure.GeminiLive.Protocol;

public class GeminiLiveClient : IGeminiLiveClient
{
    private const int SetupCompleteTimeoutMilliseconds = 3000;

    private readonly string _apiKey;
    private ClientWebSocket? _webSocket;
    private readonly CancellationTokenSource _cts = new();
    private readonly GeminiLiveInputAudioGate _inputAudioGate = new();

    public event Func<byte[], Task>? OnAudioReceived; // Raw PCM 24kHz
    public event Func<GeminiFunctionCall, Task>? OnFunctionCallReceived;
    public event Func<Task>? OnInterrupted;
    public event Func<Task>? OnInputAudioEnabled;
    public event Func<Task>? OnInputAudioDisabled;

    public GeminiLiveClient(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task ConnectAsync(
        string systemPrompt,
        object[]? tools = null,
        string? model = null,
        string? voiceName = null,
        CancellationToken cancellationToken = default)
    {
        _webSocket = new ClientWebSocket();
        var uri = new Uri($"wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent?key={_apiKey}");

        await _webSocket.ConnectAsync(uri, cancellationToken);

        var selectedModel = string.IsNullOrWhiteSpace(model) ? "models/gemini-2.5-flash-native-audio-latest" : model;
        var selectedVoice = string.IsNullOrWhiteSpace(voiceName) ? "Aoede" : voiceName;

        var setupMsg = new GeminiClientMessage
        {
            Setup = new GeminiSetup
            {
                Model = selectedModel,
                GenerationConfig = new GeminiGenerationConfig
                {
                    ResponseModalities = new[] { "AUDIO" },
                    SpeechConfig = new GeminiSpeechConfig
                    {
                        VoiceConfig = new GeminiVoiceConfig
                        {
                            PrebuiltVoiceConfig = new GeminiPrebuiltVoiceConfig
                            {
                                VoiceName = selectedVoice
                            }
                        }
                    }
                },
                SystemInstruction = new GeminiContent
                {
                    Parts = new List<GeminiPart>
                    {
                        new GeminiPart { Text = BuildGeminiSystemPrompt(systemPrompt) }
                    }
                }
            }
        };

        if (tools != null && tools.Length > 0)
        {
            setupMsg.Setup.Tools = new List<object>(tools);
        }

        await SendJsonAsync(setupMsg, cancellationToken);
        Console.WriteLine($"[GeminiLiveClient] Connected to Google Live API ({selectedModel}, voice: {selectedVoice}). Sent setup message.");
        await WaitForSetupCompleteAsync(cancellationToken);

        var initialPromptMsg = new GeminiClientMessage
        {
            ClientContent = new GeminiClientContent
            {
                Turns = new List<GeminiTurn>
                {
                    new GeminiTurn
                    {
                        Role = "user",
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = BuildInitialInterviewPrompt() }
                        }
                    }
                },
                TurnComplete = true
            }
        };

        await SendJsonAsync(initialPromptMsg, cancellationToken);
        Console.WriteLine("[GeminiLiveClient] Sent initial clientContent turn to Google.");
        _ = ReceiveLoopAsync(_cts.Token);
    }

    public async Task SendAudioChunkAsync(byte[] pcm16kAudioData, CancellationToken cancellationToken = default)
    {
        if (_webSocket?.State != WebSocketState.Open) return;

        var base64Audio = Convert.ToBase64String(pcm16kAudioData);
        var inputMsg = new GeminiClientMessage
        {
            RealtimeInput = new GeminiRealtimeInput
            {
                Audio = new GeminiRealtimeAudioChunk
                {
                    MimeType = "audio/pcm;rate=16000",
                    Data = base64Audio
                }
            }
        };

        await SendJsonAsync(inputMsg, cancellationToken);
    }

    public async Task SendToolResponseAsync(string callId, string functionName, object resultObject, CancellationToken cancellationToken = default)
    {
        if (_webSocket?.State != WebSocketState.Open) return;

        Console.WriteLine($"[GeminiLiveClient] Sending tool response for {functionName} (id: {callId})...");
        var responseMsg = new GeminiClientMessage
        {
            ToolResponse = new GeminiToolResponse
            {
                FunctionResponses = new List<GeminiFunctionResponse>
                {
                    new GeminiFunctionResponse
                    {
                        Id = callId,
                        Name = functionName,
                        Response = resultObject
                    }
                }
            }
        };

        await SendJsonAsync(responseMsg, cancellationToken);
        _inputAudioGate.OnToolResponseSent();
    }

    private async Task WaitForSetupCompleteAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(SetupCompleteTimeoutMilliseconds));

        try
        {
            while (!timeoutCts.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                var message = await ReceiveMessageAsync(timeoutCts.Token);
                if (message.MessageType == WebSocketMessageType.Close)
                {
                    throw new InvalidOperationException(
                        $"Gemini Live WebSocket closed during setup: {message.CloseStatus} {message.CloseStatusDescription}");
                }

                var msg = JsonSerializer.Deserialize<GeminiServerMessage>(message.Text);
                if (msg?.Error != null)
                {
                    throw new InvalidOperationException(
                        $"Gemini Live setup failed: Code {msg.Error.Code} ({msg.Error.Status}): {msg.Error.Message}");
                }

                if (msg?.SetupComplete != null)
                {
                    Console.WriteLine("[GeminiLiveClient] Setup complete received from Google.");
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Gemini Live setup did not complete within {SetupCompleteTimeoutMilliseconds} ms.");
        }

        throw new InvalidOperationException("Gemini Live setup did not complete before the WebSocket closed.");
    }

    private async Task SendJsonAsync(object obj, CancellationToken cancellationToken)
    {
        if (_webSocket?.State != WebSocketState.Open) return;
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
        {
            try
            {
                var message = await ReceiveMessageAsync(cancellationToken);
                if (message.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine($"[GeminiLiveClient] Google WebSocket closed by server. Code: {message.CloseStatus}, Description: {message.CloseStatusDescription}");
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                await ProcessServerMessageAsync(message.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GeminiLiveClient] Receive loop error: {ex.Message}");
                break;
            }
        }
    }

    private async Task<ReceivedWebSocketMessage> ReceiveMessageAsync(CancellationToken cancellationToken)
    {
        if (_webSocket is null)
        {
            throw new InvalidOperationException("Gemini Live WebSocket is not connected.");
        }

        var buffer = new byte[64 * 1024];
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            ms.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return new ReceivedWebSocketMessage(
            result.MessageType,
            Encoding.UTF8.GetString(ms.ToArray()),
            result.CloseStatus,
            result.CloseStatusDescription);
    }

    private async Task ProcessServerMessageAsync(string json)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<GeminiServerMessage>(json);
            if (msg == null) return;

            if (msg.Error != null)
            {
                Console.WriteLine($"[GeminiLiveClient] ERROR from Google Live API: Code {msg.Error.Code} ({msg.Error.Status}): {msg.Error.Message}");
            }

            var functionCalls = msg.ToolCall?.FunctionCalls;
            if (functionCalls is { Count: > 0 })
            {
                await ApplyInputAudioGateDecisionAsync(_inputAudioGate.OnToolCall(functionCalls.Count));
            }

            var parts = msg.ServerContent?.ModelTurn?.Parts;
            if (parts != null)
            {
                foreach (var part in parts)
                {
                    if (part.InlineData != null && !string.IsNullOrEmpty(part.InlineData.Data))
                    {
                        var audioBytes = Convert.FromBase64String(part.InlineData.Data);
                        await InvokeHandlersAsync(OnAudioReceived, audioBytes);
                    }
                }
            }

            if (msg.ServerContent?.Interrupted == true)
            {
                Console.WriteLine("[GeminiLiveClient] Interrupted signal received.");
                await InvokeHandlersAsync(OnInterrupted);
            }

            if (msg.ServerContent?.TurnComplete == true)
            {
                await ApplyInputAudioGateDecisionAsync(_inputAudioGate.OnAssistantTurnComplete());
            }

            if (functionCalls != null)
            {
                foreach (var call in functionCalls)
                {
                    Console.WriteLine($"[GeminiLiveClient] Function call received: {call.Name} (id: {call.Id})");
                    await InvokeHandlersAsync(OnFunctionCallReceived, call);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GeminiLiveClient] Error parsing message: {ex.Message}");
        }
    }

    public async Task DisconnectAsync()
    {
        _cts.Cancel();
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", CancellationToken.None);
            }
            catch (Exception) { }
        }
        _webSocket?.Dispose();
    }

    private sealed record ReceivedWebSocketMessage(
        WebSocketMessageType MessageType,
        string Text,
        WebSocketCloseStatus? CloseStatus,
        string? CloseStatusDescription);

    public static string BuildInitialInterviewPrompt()
    {
        return """
            Розпочни інтерв'ю реєстрації HFU зараз. Розмовляй українською мовою.
            Для першого ходу стан реєстрації вже відомий: це нова порожня демо-сесія.
            Не використовуй інструменти перед першим питанням.
            Коротко привітайся, поясни, що це голосова демо-реєстрація HFU,
            зроби коротку природну паузу і одразу попроси користувача назвати ім'я для анкети.
            """;
    }

    private static string BuildGeminiSystemPrompt(string systemPrompt)
    {
        return
            $"""
            {systemPrompt}

            Gemini Live startup rule:
            For the first assistant turn only, the backend state is already known to be a fresh empty demo session.
            Do not use tools before the first spoken question. Greet briefly in Ukrainian and ask for the user's given name directly.
            After the user answers, follow the regular tool policy.
            """;
    }

    private async Task ApplyInputAudioGateDecisionAsync(GeminiInputAudioGateDecision decision)
    {
        switch (decision)
        {
            case GeminiInputAudioGateDecision.Enable:
                Console.WriteLine("[GeminiLiveClient] Assistant turn complete. Input audio enabled.");
                await InvokeHandlersAsync(OnInputAudioEnabled);
                break;
            case GeminiInputAudioGateDecision.Disable:
                Console.WriteLine("[GeminiLiveClient] Tool call pending. Input audio disabled.");
                await InvokeHandlersAsync(OnInputAudioDisabled);
                break;
        }
    }

    private static async Task InvokeHandlersAsync<T>(Func<T, Task>? handlers, T arg)
    {
        if (handlers == null)
        {
            return;
        }

        foreach (Func<T, Task> handler in handlers.GetInvocationList())
        {
            await handler(arg);
        }
    }

    private static async Task InvokeHandlersAsync(Func<Task>? handlers)
    {
        if (handlers == null)
        {
            return;
        }

        foreach (Func<Task> handler in handlers.GetInvocationList())
        {
            await handler();
        }
    }
}
