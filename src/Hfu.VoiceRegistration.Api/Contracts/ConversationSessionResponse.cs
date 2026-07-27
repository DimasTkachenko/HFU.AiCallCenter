using Hfu.VoiceRegistration.Application.RegistrationTools;
using Hfu.VoiceRegistration.Domain.Conversations;

namespace Hfu.VoiceRegistration.Api.Contracts;

public sealed record ConversationSessionResponse(
    Guid SessionId,
    ConversationSessionStatus Status,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    string? RealtimeConnectionId,
    RegistrationResult? RegistrationResult,
    RegistrationStateSnapshot State,
    IReadOnlyList<ConversationEventResponse> Events);
