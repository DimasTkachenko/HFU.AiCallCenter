using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Domain.Tests.Registration;

public sealed class RegistrationDraftTests
{
    [Fact]
    public void NewFieldStartsMissingWithoutAValue()
    {
        var field = RegistrationField<string>.Missing();

        Assert.Null(field.Value);
        Assert.Null(field.RawValue);
        Assert.Equal(RegistrationFieldStatus.Missing, field.Status);
    }

    [Fact]
    public void CapturedFieldKeepsNormalizedAndRawValues()
    {
        var field = RegistrationField<string>.Captured("Kharkiv region", " харьковская область ");

        Assert.Equal("Kharkiv region", field.Value);
        Assert.Equal(" харьковская область ", field.RawValue);
        Assert.Equal(RegistrationFieldStatus.Captured, field.Status);
    }

    [Fact]
    public void ConfirmedFieldIsAlreadyReadyForCompletionChecks()
    {
        var field = RegistrationField<DateOnly>.Confirmed(
            new DateOnly(1991, 8, 24),
            "24.08.1991");

        Assert.Equal(new DateOnly(1991, 8, 24), field.Value);
        Assert.Equal("24.08.1991", field.RawValue);
        Assert.Equal(RegistrationFieldStatus.Confirmed, field.Status);
    }

    [Fact]
    public void RejectedOptionalFieldCarriesNoValue()
    {
        var field = RegistrationField<string>.Rejected();

        Assert.Null(field.Value);
        Assert.Null(field.RawValue);
        Assert.Equal(RegistrationFieldStatus.Rejected, field.Status);
    }

    [Fact]
    public void NewDraftInitializesEveryRegistrationFieldAsMissing()
    {
        var draft = RegistrationDraft.Create();

        Assert.Equal(RegistrationFieldStatus.Missing, draft.FirstName.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, draft.LastName.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, draft.Patronymic.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, draft.DateOfBirth.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, draft.PhoneNumber.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, draft.Email.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, draft.CurrentRegion.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, draft.CurrentCity.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, draft.ActualAddress.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, draft.UserCategory.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, draft.RegionBeforeWar.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, draft.DisplacedCertificateYear.Status);
        Assert.False(draft.PersonalDataConsent);
        Assert.False(draft.RegistrationConfirmed);
    }

    [Fact]
    public void UserCategoryContainsAllSupportedSpecificationValues()
    {
        var categories = Enum.GetNames<UserCategory>();

        Assert.Contains(nameof(UserCategory.InternallyDisplacedPerson), categories);
        Assert.Contains(nameof(UserCategory.HasManyChildren), categories);
        Assert.Contains(nameof(UserCategory.DisabledPerson), categories);
        Assert.Contains(nameof(UserCategory.MilitaryPerson), categories);
        Assert.Contains(nameof(UserCategory.MilitaryPersonRelative), categories);
        Assert.Contains(nameof(UserCategory.Other), categories);
    }
}
