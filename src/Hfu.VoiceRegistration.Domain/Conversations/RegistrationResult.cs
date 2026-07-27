namespace Hfu.VoiceRegistration.Domain.Conversations;

public sealed record RegistrationResult(
    string RegistrationId,
    DateTimeOffset CompletedAt);
