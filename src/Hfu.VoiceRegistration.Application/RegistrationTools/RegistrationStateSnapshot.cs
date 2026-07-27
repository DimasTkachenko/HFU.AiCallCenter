using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public sealed record RegistrationStateSnapshot(
    Guid SessionId,
    long Version,
    IReadOnlyList<RegistrationFieldSnapshot> Fields,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> FieldsRequiringClarification,
    IReadOnlyList<string> FieldsAwaitingConfirmation,
    bool RegistrationCanBeCompleted,
    IReadOnlyList<RegistrationValidationIssue> CompletionIssues);
