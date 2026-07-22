namespace Hfu.VoiceRegistration.Api.Realtime;

public enum ConversationRealtimeEventType
{
    SessionCreated,
    SessionUpdated,
    RegistrationStateChanged,
    RegistrationToolCompleted,
    RegistrationCompleted,
    SessionAbandoned,
    DiagnosticEventAdded,
    TranscriptReceived,
    ToolCallReceived,
    ToolCallCompleted,
    ValidationFailed,
    ConnectionStatusChanged
}
