namespace Hfu.VoiceRegistration.Domain.Registration;

public sealed record RegistrationDraft
{
    private RegistrationDraft()
    {
    }

    public RegistrationField<string> FirstName { get; init; } = RegistrationField<string>.Missing();

    public RegistrationField<string> LastName { get; init; } = RegistrationField<string>.Missing();

    public RegistrationField<string> Patronymic { get; init; } = RegistrationField<string>.Missing();

    public RegistrationField<DateOnly> DateOfBirth { get; init; } = RegistrationField<DateOnly>.Missing();

    public RegistrationField<string> PhoneNumber { get; init; } = RegistrationField<string>.Missing();

    public RegistrationField<string> Email { get; init; } = RegistrationField<string>.Missing();

    public RegistrationField<string> CurrentRegion { get; init; } = RegistrationField<string>.Missing();

    public RegistrationField<string> CurrentCity { get; init; } = RegistrationField<string>.Missing();

    public RegistrationField<string> ActualAddress { get; init; } = RegistrationField<string>.Missing();

    public RegistrationField<UserCategory> UserCategory { get; init; } =
        RegistrationField<UserCategory>.Missing();

    public RegistrationField<string> RegionBeforeWar { get; init; } = RegistrationField<string>.Missing();

    public RegistrationField<int> DisplacedCertificateYear { get; init; } =
        RegistrationField<int>.Missing();

    public bool PersonalDataConsent { get; init; }

    public bool RegistrationConfirmed { get; init; }

    public static RegistrationDraft Create()
    {
        return new RegistrationDraft();
    }
}
