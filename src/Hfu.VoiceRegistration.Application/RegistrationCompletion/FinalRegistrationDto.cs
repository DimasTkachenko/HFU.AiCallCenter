using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Application.RegistrationCompletion;

public sealed record FinalRegistrationDto(
    string FirstName,
    string LastName,
    string? Patronymic,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string? Email,
    string CurrentRegion,
    string? CurrentRegionReferenceId,
    string CurrentCity,
    string? ActualAddress,
    UserCategory UserCategory,
    string? RegionBeforeWar,
    string? RegionBeforeWarReferenceId,
    int? DisplacedCertificateYear,
    bool PersonalDataConsent,
    bool RegistrationConfirmed);
