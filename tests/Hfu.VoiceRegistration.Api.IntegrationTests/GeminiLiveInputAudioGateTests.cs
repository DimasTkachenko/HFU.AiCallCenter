using Hfu.VoiceRegistration.Infrastructure.GeminiLive;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class GeminiLiveInputAudioGateTests
{
    [Fact]
    public void AssistantTurnCompleteEnablesInputWhenNoToolCallIsPending()
    {
        var gate = new GeminiLiveInputAudioGate();

        var decision = gate.OnAssistantTurnComplete();

        Assert.Equal(GeminiInputAudioGateDecision.Enable, decision);
    }

    [Fact]
    public void ToolCallDisablesInputUntilAssistantCompletesAfterToolResponse()
    {
        var gate = new GeminiLiveInputAudioGate();

        Assert.Equal(GeminiInputAudioGateDecision.Enable, gate.OnAssistantTurnComplete());
        Assert.Equal(GeminiInputAudioGateDecision.Disable, gate.OnToolCall());
        Assert.Equal(GeminiInputAudioGateDecision.None, gate.OnAssistantTurnComplete());

        gate.OnToolResponseSent();

        Assert.Equal(GeminiInputAudioGateDecision.Enable, gate.OnAssistantTurnComplete());
    }
}
