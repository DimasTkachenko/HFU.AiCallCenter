namespace Hfu.VoiceRegistration.Domain.Registration;

public sealed record RegistrationValidationIssue(
    string Code,
    string Field,
    string Message);
