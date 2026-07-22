namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public sealed record RegistrationFieldUpdate(
    string Name,
    object? Value,
    string? RawValue = null);
