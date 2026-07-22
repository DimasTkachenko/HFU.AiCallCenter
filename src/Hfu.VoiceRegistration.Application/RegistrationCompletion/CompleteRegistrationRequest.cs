namespace Hfu.VoiceRegistration.Application.RegistrationCompletion;

public sealed record CompleteRegistrationRequest(
    bool PersonalDataConsent,
    bool RegistrationConfirmed);
