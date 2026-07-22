using Hfu.VoiceRegistration.Application.RegistrationCompletion;

namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public interface IRegistrationToolService
{
    Task<RegistrationToolResult> UpdateRegistrationFieldsAsync(
        Guid sessionId,
        IReadOnlyCollection<RegistrationFieldUpdate> fields,
        CancellationToken cancellationToken);

    Task<RegistrationToolResult> ConfirmRegistrationFieldsAsync(
        Guid sessionId,
        IReadOnlyCollection<string> fieldNames,
        CancellationToken cancellationToken);

    Task<RegistrationToolResult> MarkFieldsForClarificationAsync(
        Guid sessionId,
        IReadOnlyCollection<string> fieldNames,
        string? reason,
        CancellationToken cancellationToken);

    Task<RegistrationToolResult> ClearRegistrationFieldsAsync(
        Guid sessionId,
        IReadOnlyCollection<string> fieldNames,
        CancellationToken cancellationToken);

    Task<RegistrationToolResult> GetRegistrationStateAsync(
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<RegistrationToolResult> CompleteRegistrationAsync(
        Guid sessionId,
        CompleteRegistrationRequest request,
        CancellationToken cancellationToken);
}
