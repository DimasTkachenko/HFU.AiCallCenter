namespace Hfu.VoiceRegistration.Api.Realtime;

public interface IConversationRealtimeClient
{
    Task ConversationEvent(ConversationRealtimeEvent conversationEvent);
}
