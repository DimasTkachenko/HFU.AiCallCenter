namespace Hfu.VoiceRegistration.Api.GeminiLive;

using Hfu.VoiceRegistration.Infrastructure.GeminiLive.Protocol;
using System.Threading.Tasks;
using System;
using System.Threading;

public interface IGeminiLiveClient
{
    event Func<byte[], Task>? OnAudioReceived;
    event Func<GeminiFunctionCall, Task>? OnFunctionCallReceived;
    event Func<Task>? OnInterrupted;
    event Func<Task>? OnInputAudioEnabled;
    event Func<Task>? OnInputAudioDisabled;

    Task ConnectAsync(string systemPrompt, object[]? tools = null, string? model = null, string? voiceName = null, CancellationToken cancellationToken = default);
    Task SendAudioChunkAsync(byte[] pcm16kAudioData, CancellationToken cancellationToken = default);
    Task SendToolResponseAsync(string callId, string functionName, object resultObject, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
}
