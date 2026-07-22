namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public sealed record RegistrationToolError(
    string Code,
    string? Field,
    string Message,
    IReadOnlyList<string> Suggestions)
{
    public RegistrationToolError(
        string code,
        string? field,
        string message)
        : this(code, field, message, Array.Empty<string>())
    {
    }
}
