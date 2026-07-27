namespace Hfu.VoiceRegistration.Domain.Conversations;

public enum ConversationSessionStatus
{
    Created,
    Connecting,
    Active,
    Completing,
    Completed,
    Failed,
    Abandoned
}
