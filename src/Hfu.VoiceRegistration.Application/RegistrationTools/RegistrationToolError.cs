namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public sealed record RegistrationToolError(
    string Code,
    string? Field,
    string Message);
