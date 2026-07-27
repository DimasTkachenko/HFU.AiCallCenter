using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Application.RegistrationTools;

namespace Hfu.VoiceRegistration.Api.Contracts;

public sealed record UpdateRegistrationFieldsHttpRequest
{
    public IReadOnlyList<RegistrationFieldUpdate> Fields { get; init; } =
        Array.Empty<RegistrationFieldUpdate>();
}

public sealed record ConfirmRegistrationFieldsHttpRequest
{
    public IReadOnlyList<string> FieldNames { get; init; } =
        Array.Empty<string>();
}

public sealed record MarkFieldsForClarificationHttpRequest
{
    public IReadOnlyList<string> FieldNames { get; init; } =
        Array.Empty<string>();

    public string? Reason { get; init; }
}

public sealed record ClearRegistrationFieldsHttpRequest
{
    public IReadOnlyList<string> FieldNames { get; init; } =
        Array.Empty<string>();
}

public sealed record CompleteRegistrationHttpRequest(
    bool PersonalDataConsent,
    bool RegistrationConfirmed)
{
    public CompleteRegistrationRequest ToApplicationRequest()
    {
        return new CompleteRegistrationRequest(
            PersonalDataConsent,
            RegistrationConfirmed);
    }
}
