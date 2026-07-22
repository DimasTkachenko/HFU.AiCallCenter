namespace Hfu.VoiceRegistration.Api.OpenAIRealtime;

public sealed class OpenAIRealtimeConfigurationException : Exception
{
    public OpenAIRealtimeConfigurationException(string message)
        : base(message)
    {
    }
}

public sealed class OpenAIRealtimeApiException : Exception
{
    public OpenAIRealtimeApiException(int statusCode, string responseBody)
        : base($"OpenAI Realtime request failed with HTTP {statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public int StatusCode { get; }

    public string ResponseBody { get; }
}
