namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public sealed record RegistrationRecommendedNextAction(
    string Type,
    string? FieldName,
    string Instruction);
