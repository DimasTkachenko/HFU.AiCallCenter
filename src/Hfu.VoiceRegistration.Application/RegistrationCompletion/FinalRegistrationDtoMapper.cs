using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Application.RegistrationCompletion;

public static class FinalRegistrationDtoMapper
{
    public static FinalRegistrationDto Map(RegistrationDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var isInternallyDisplaced =
            draft.UserCategory.Value == UserCategory.InternallyDisplacedPerson;

        return new FinalRegistrationDto(
            FirstName: Required(draft.FirstName),
            LastName: Required(draft.LastName),
            Patronymic: Optional(draft.Patronymic),
            DateOfBirth: Required(draft.DateOfBirth),
            PhoneNumber: Required(draft.PhoneNumber),
            Email: Optional(draft.Email),
            CurrentRegion: Required(draft.CurrentRegion),
            CurrentRegionReferenceId: draft.CurrentRegion.ReferenceId,
            CurrentCity: Required(draft.CurrentCity),
            ActualAddress: Optional(draft.ActualAddress),
            UserCategory: Required(draft.UserCategory),
            RegionBeforeWar: isInternallyDisplaced ? Required(draft.RegionBeforeWar) : null,
            RegionBeforeWarReferenceId: isInternallyDisplaced ? draft.RegionBeforeWar.ReferenceId : null,
            DisplacedCertificateYear: isInternallyDisplaced
                ? Required(draft.DisplacedCertificateYear)
                : null,
            PersonalDataConsent: draft.PersonalDataConsent,
            RegistrationConfirmed: draft.RegistrationConfirmed);
    }

    private static T Required<T>(RegistrationField<T> field)
    {
        return field.Value
            ?? throw new InvalidOperationException("Required registration field does not have a value.");
    }

    private static string? Optional(RegistrationField<string> field)
    {
        return string.IsNullOrWhiteSpace(field.Value)
            ? null
            : field.Value;
    }
}
