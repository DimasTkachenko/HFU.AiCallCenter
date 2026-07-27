namespace Hfu.VoiceRegistration.Infrastructure.GeminiLive;

public enum GeminiInputAudioGateDecision
{
    None,
    Enable,
    Disable
}

public sealed class GeminiLiveInputAudioGate
{
    private bool _inputAudioEnabled;
    private int _pendingToolResponses;

    public GeminiInputAudioGateDecision OnAssistantTurnComplete()
    {
        if (_inputAudioEnabled || _pendingToolResponses > 0)
        {
            return GeminiInputAudioGateDecision.None;
        }

        _inputAudioEnabled = true;
        return GeminiInputAudioGateDecision.Enable;
    }

    public GeminiInputAudioGateDecision OnToolCall(int functionCallCount = 1)
    {
        _pendingToolResponses += Math.Max(1, functionCallCount);

        if (!_inputAudioEnabled)
        {
            return GeminiInputAudioGateDecision.None;
        }

        _inputAudioEnabled = false;
        return GeminiInputAudioGateDecision.Disable;
    }

    public void OnToolResponseSent()
    {
        if (_pendingToolResponses > 0)
        {
            _pendingToolResponses--;
        }
    }
}
