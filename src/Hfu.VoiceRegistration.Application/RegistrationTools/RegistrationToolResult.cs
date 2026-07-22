namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public sealed record RegistrationToolResult(
    RegistrationStateSnapshot? State,
    IReadOnlyList<RegistrationToolError> Errors,
    RegistrationCompletionDetails? Completion = null)
{
    public bool Succeeded => Errors.Count == 0;

    public static RegistrationToolResult Success(
        RegistrationStateSnapshot state,
        RegistrationCompletionDetails? completion = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new RegistrationToolResult(
            state,
            Array.Empty<RegistrationToolError>(),
            completion);
    }

    public static RegistrationToolResult Failure(
        RegistrationStateSnapshot? state,
        IReadOnlyList<RegistrationToolError> errors,
        RegistrationCompletionDetails? completion = null)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new RegistrationToolResult(state, errors, completion);
    }
}
