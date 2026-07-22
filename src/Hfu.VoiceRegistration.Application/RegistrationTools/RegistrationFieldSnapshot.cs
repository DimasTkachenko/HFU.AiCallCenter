using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public sealed record RegistrationFieldSnapshot(
    string Name,
    object? Value,
    string? RawValue,
    RegistrationFieldStatus Status,
    string? ClarificationReason);
