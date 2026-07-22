namespace Hfu.VoiceRegistration.Domain.Registration;

public sealed record RegistrationValidationResult(IReadOnlyList<RegistrationValidationIssue> Issues)
{
    public bool CanComplete => Issues.Count == 0;
}
