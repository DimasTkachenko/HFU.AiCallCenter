using System.Net.WebSockets;
using Hfu.VoiceRegistration.Api.GeminiLive;
using Hfu.VoiceRegistration.Api.Realtime;
using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Application.RegistrationTools;
using Hfu.VoiceRegistration.Domain.Conversations;
using Hfu.VoiceRegistration.Infrastructure.GeminiLive.Protocol;
using Microsoft.Extensions.Options;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class GeminiLiveSessionHandlerTests
{
    [Fact]
    public async Task HandleSessionAsyncDoesNotForwardClientAudioBeforeInputAudioIsEnabled()
    {
        var session = ConversationSession.Create(DateTimeOffset.Parse("2026-07-27T12:00:00Z"));
        var geminiClient = new CapturingGeminiLiveClient();
        var clientWebSocket = ScriptedClientWebSocket.WithIncoming(
            (WebSocketMessageType.Binary, new byte[] { 1, 2, 3 }),
            (WebSocketMessageType.Close, Array.Empty<byte>()));
        var handler = CreateHandler(session, geminiClient);

        await handler.HandleSessionAsync(session.SessionId, clientWebSocket, CancellationToken.None);

        Assert.Empty(geminiClient.AudioChunks);
        Assert.DoesNotContain(
            clientWebSocket.SentBinaryFrames,
            frame => frame.SequenceEqual(new byte[] { 0x03 }));
    }

    [Fact]
    public async Task HandleSessionAsyncForwardsClientAudioAfterInputAudioIsEnabled()
    {
        var session = ConversationSession.Create(DateTimeOffset.Parse("2026-07-27T12:00:00Z"));
        var geminiClient = new CapturingGeminiLiveClient
        {
            EnableInputAudioOnConnect = true
        };
        var clientWebSocket = ScriptedClientWebSocket.WithIncoming(
            (WebSocketMessageType.Binary, new byte[] { 4, 5, 6 }),
            (WebSocketMessageType.Close, Array.Empty<byte>()));
        var handler = CreateHandler(session, geminiClient);

        await handler.HandleSessionAsync(session.SessionId, clientWebSocket, CancellationToken.None);

        var audioChunk = Assert.Single(geminiClient.AudioChunks);
        Assert.Equal(new byte[] { 4, 5, 6 }, audioChunk);
        Assert.Contains(
            clientWebSocket.SentBinaryFrames,
            frame => frame.SequenceEqual(new byte[] { 0x03 }));
    }

    [Fact]
    public async Task HandleSessionAsyncDoesNotForwardClientAudioAfterInputAudioIsDisabled()
    {
        var session = ConversationSession.Create(DateTimeOffset.Parse("2026-07-27T12:00:00Z"));
        var geminiClient = new CapturingGeminiLiveClient
        {
            EnableInputAudioOnConnect = true,
            DisableInputAudioAfterEnableOnConnect = true
        };
        var clientWebSocket = ScriptedClientWebSocket.WithIncoming(
            (WebSocketMessageType.Binary, new byte[] { 7, 8, 9 }),
            (WebSocketMessageType.Close, Array.Empty<byte>()));
        var handler = CreateHandler(session, geminiClient);

        await handler.HandleSessionAsync(session.SessionId, clientWebSocket, CancellationToken.None);

        Assert.Empty(geminiClient.AudioChunks);
        Assert.Contains(
            clientWebSocket.SentBinaryFrames,
            frame => frame.SequenceEqual(new byte[] { 0x04 }));
    }

    private static GeminiLiveSessionHandler CreateHandler(
        ConversationSession session,
        CapturingGeminiLiveClient geminiClient)
    {
        return new GeminiLiveSessionHandler(
            geminiClient,
            new SingleConversationSessionStore(session),
            new ThrowingRegistrationToolService(),
            new NoOpRealtimeNotifier(),
            Options.Create(new GeminiLiveOptions
            {
                ApiKey = "gemini-test-key"
            }));
    }

    private sealed class CapturingGeminiLiveClient : IGeminiLiveClient
    {
        public event Func<byte[], Task>? OnAudioReceived;

        public event Func<GeminiFunctionCall, Task>? OnFunctionCallReceived;

        public event Func<Task>? OnInterrupted;

        public event Func<Task>? OnInputAudioEnabled;

        public event Func<Task>? OnInputAudioDisabled;

        public bool EnableInputAudioOnConnect { get; init; }

        public bool DisableInputAudioAfterEnableOnConnect { get; init; }

        public List<byte[]> AudioChunks { get; } = new();

        public async Task ConnectAsync(
            string systemPrompt,
            object[]? tools = null,
            string? model = null,
            string? voiceName = null,
            CancellationToken cancellationToken = default)
        {
            if (EnableInputAudioOnConnect)
            {
                await InvokeHandlersAsync(OnInputAudioEnabled);
            }

            if (DisableInputAudioAfterEnableOnConnect)
            {
                await InvokeHandlersAsync(OnInputAudioDisabled);
            }
        }

        public Task SendAudioChunkAsync(
            byte[] pcm16kAudioData,
            CancellationToken cancellationToken = default)
        {
            AudioChunks.Add(pcm16kAudioData);

            return Task.CompletedTask;
        }

        public Task SendToolResponseAsync(
            string callId,
            string functionName,
            object resultObject,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public async Task RaiseAudioReceivedAsync(byte[] audio)
        {
            await InvokeHandlersAsync(OnAudioReceived, audio);
        }

        public async Task RaiseFunctionCallReceivedAsync(GeminiFunctionCall functionCall)
        {
            await InvokeHandlersAsync(OnFunctionCallReceived, functionCall);
        }

        public async Task RaiseInterruptedAsync()
        {
            await InvokeHandlersAsync(OnInterrupted);
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

    private sealed class SingleConversationSessionStore : IConversationSessionStore
    {
        private readonly ConversationSession _session;

        public SingleConversationSessionStore(ConversationSession session)
        {
            _session = session;
        }

        public Task<ConversationSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(sessionId == _session.SessionId ? _session : null);
        }

        public Task CreateAsync(ConversationSession session, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(ConversationSession session, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ConversationSession> UpdateAsync(
            Guid sessionId,
            Func<ConversationSession, CancellationToken, Task<ConversationSession>> mutate,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ThrowingRegistrationToolService : IRegistrationToolService
    {
        public Task<RegistrationToolResult> UpdateRegistrationFieldsAsync(
            Guid sessionId,
            IReadOnlyCollection<RegistrationFieldUpdate> fields,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegistrationToolResult> ConfirmRegistrationFieldsAsync(
            Guid sessionId,
            IReadOnlyCollection<string> fieldNames,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegistrationToolResult> MarkFieldsForClarificationAsync(
            Guid sessionId,
            IReadOnlyCollection<string> fieldNames,
            string? reason,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegistrationToolResult> ClearRegistrationFieldsAsync(
            Guid sessionId,
            IReadOnlyCollection<string> fieldNames,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegistrationToolResult> GetRegistrationStateAsync(
            Guid sessionId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegistrationToolResult> CompleteRegistrationAsync(
            Guid sessionId,
            CompleteRegistrationRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class NoOpRealtimeNotifier : IConversationRealtimeNotifier
    {
        public Task NotifyAsync(
            Guid sessionId,
            long version,
            ConversationRealtimeEventType type,
            string message,
            CancellationToken cancellationToken,
            string? correlationId = null)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ScriptedClientWebSocket : WebSocket
    {
        private readonly Queue<(WebSocketMessageType MessageType, byte[] Payload)> _incoming;
        private WebSocketState _state = WebSocketState.Open;

        private ScriptedClientWebSocket(
            Queue<(WebSocketMessageType MessageType, byte[] Payload)> incoming)
        {
            _incoming = incoming;
        }

        public static ScriptedClientWebSocket WithIncoming(
            params (WebSocketMessageType MessageType, byte[] Payload)[] incoming)
        {
            return new ScriptedClientWebSocket(new Queue<(WebSocketMessageType, byte[])>(incoming));
        }

        public List<byte[]> SentBinaryFrames { get; } = new();

        public override WebSocketCloseStatus? CloseStatus { get; } = null;

        public override string? CloseStatusDescription { get; } = null;

        public override WebSocketState State => _state;

        public override string? SubProtocol => null;

        public override void Abort()
        {
            _state = WebSocketState.Aborted;
        }

        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;

            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            _state = WebSocketState.CloseSent;

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _state = WebSocketState.Closed;
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            if (!_incoming.TryDequeue(out var next))
            {
                _state = WebSocketState.CloseReceived;

                return Task.FromResult(new WebSocketReceiveResult(
                    0,
                    WebSocketMessageType.Close,
                    true,
                    WebSocketCloseStatus.NormalClosure,
                    "done"));
            }

            if (next.MessageType == WebSocketMessageType.Close)
            {
                _state = WebSocketState.CloseReceived;

                return Task.FromResult(new WebSocketReceiveResult(
                    0,
                    WebSocketMessageType.Close,
                    true,
                    WebSocketCloseStatus.NormalClosure,
                    "done"));
            }

            Buffer.BlockCopy(next.Payload, 0, buffer.Array!, buffer.Offset, next.Payload.Length);

            return Task.FromResult(new WebSocketReceiveResult(
                next.Payload.Length,
                next.MessageType,
                true));
        }

        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            if (messageType == WebSocketMessageType.Binary)
            {
                SentBinaryFrames.Add(buffer.ToArray());
            }

            return Task.CompletedTask;
        }
    }
}
