namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public sealed record RegistrationToolResult(
    RegistrationStateSnapshot? State,
    IReadOnlyList<RegistrationToolError> Errors)
{
    public bool Succeeded => Errors.Count == 0;

    public static RegistrationToolResult Success(RegistrationStateSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new RegistrationToolResult(state, Array.Empty<RegistrationToolError>());
    }

    public static RegistrationToolResult Failure(
        RegistrationStateSnapshot? state,
        IReadOnlyList<RegistrationToolError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new RegistrationToolResult(state, errors);
    }
}
