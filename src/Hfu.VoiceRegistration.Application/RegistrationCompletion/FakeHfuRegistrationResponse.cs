namespace Hfu.VoiceRegistration.Application.RegistrationCompletion;

public sealed record FakeHfuRegistrationResponse(
    bool Success,
    string RegistrationId,
    string Message,
    DateTimeOffset CompletedAt);
