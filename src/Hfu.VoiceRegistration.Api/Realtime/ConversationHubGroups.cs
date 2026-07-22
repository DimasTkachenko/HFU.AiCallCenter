namespace Hfu.VoiceRegistration.Api.Realtime;

public static class ConversationHubGroups
{
    public static string ForSession(Guid sessionId)
    {
        return $"conversation-session:{sessionId:D}";
    }
}
