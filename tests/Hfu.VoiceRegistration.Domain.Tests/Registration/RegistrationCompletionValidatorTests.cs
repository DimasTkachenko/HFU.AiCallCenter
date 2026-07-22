using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Domain.Tests.Registration;

public sealed class RegistrationCompletionValidatorTests
{
    [Fact]
    public void CompleteDraftCanBeCompleted()
    {
        var result = RegistrationCompletionValidator.Evaluate(CompleteDraft());

        Assert.True(result.CanComplete);
        Assert.Empty(result.Issues);
    }

    [Theory]
    [MemberData(nameof(RequiredFieldCases))]
    public void RequiredFieldMustBeFilled(
        string expectedFieldName,
        Func<RegistrationDraft, RegistrationDraft> removeRequiredField)
    {
        var draft = removeRequiredField(CompleteDraft());

        var result = RegistrationCompletionValidator.Evaluate(draft);

        Assert.False(result.CanComplete);
        Assert.Contains(result.Issues, issue => issue.Field == expectedFieldName);
    }

    [Fact]
    public void RequiredRejectedFieldBlocksCompletion()
    {
        var draft = CompleteDraft() with
        {
            FirstName = RegistrationField<string>.Rejected()
        };

        var result = RegistrationCompletionValidator.Evaluate(draft);

        Assert.False(result.CanComplete);
        Assert.Contains(result.Issues, issue => issue.Field == RegistrationFieldNames.FirstName);
    }

    [Fact]
    public void RequiredFieldNeedingClarificationBlocksCompletion()
    {
        var draft = CompleteDraft() with
        {
            LastName = RegistrationField<string>.NeedsClarification("Tkachenko", "Tka?")
        };

        var result = RegistrationCompletionValidator.Evaluate(draft);

        Assert.False(result.CanComplete);
        Assert.Contains(result.Issues, issue => issue.Field == RegistrationFieldNames.LastName);
    }

    [Fact]
    public void OptionalMissingAndRejectedFieldsDoNotBlockCompletion()
    {
        var draft = CompleteDraft() with
        {
            Patronymic = RegistrationField<string>.Rejected(),
            Email = RegistrationField<string>.Rejected(),
            ActualAddress = RegistrationField<string>.Missing()
        };

        var result = RegistrationCompletionValidator.Evaluate(draft);

        Assert.True(result.CanComplete);
    }

    [Fact]
    public void CapturedEmailBlocksCompletionUntilConfirmed()
    {
        var draft = CompleteDraft() with
        {
            Email = RegistrationField<string>.Captured("person@example.com")
        };

        var result = RegistrationCompletionValidator.Evaluate(draft);

        Assert.False(result.CanComplete);
        Assert.Contains(
            result.Issues,
            issue => issue.Field == RegistrationFieldNames.Email
                && issue.Code == RegistrationValidationCodes.RequiresConfirmation);
    }

    [Theory]
    [MemberData(nameof(ConservativeConfirmationFieldCases))]
    public void ConservativeConfirmationFieldsMustBeConfirmed(
        string expectedFieldName,
        Func<RegistrationDraft, RegistrationDraft> makeCaptured)
    {
        var draft = makeCaptured(CompleteDraft());

        var result = RegistrationCompletionValidator.Evaluate(draft);

        Assert.False(result.CanComplete);
        Assert.Contains(
            result.Issues,
            issue => issue.Field == expectedFieldName
                && issue.Code == RegistrationValidationCodes.RequiresConfirmation);
    }

    [Fact]
    public void InternallyDisplacedPersonRequiresPreviousRegionAndCertificateYear()
    {
        var draft = CompleteDraft() with
        {
            UserCategory = RegistrationField<UserCategory>.Confirmed(UserCategory.InternallyDisplacedPerson),
            RegionBeforeWar = RegistrationField<string>.Missing(),
            DisplacedCertificateYear = RegistrationField<int>.Missing()
        };

        var result = RegistrationCompletionValidator.Evaluate(draft);

        Assert.False(result.CanComplete);
        Assert.Contains(result.Issues, issue => issue.Field == RegistrationFieldNames.RegionBeforeWar);
        Assert.Contains(result.Issues, issue => issue.Field == RegistrationFieldNames.DisplacedCertificateYear);
    }

    [Fact]
    public void NonInternallyDisplacedPersonDoesNotRequireIdpSpecificFields()
    {
        var draft = CompleteDraft() with
        {
            UserCategory = RegistrationField<UserCategory>.Confirmed(UserCategory.Other),
            RegionBeforeWar = RegistrationField<string>.Missing(),
            DisplacedCertificateYear = RegistrationField<int>.Missing()
        };

        var result = RegistrationCompletionValidator.Evaluate(draft);

        Assert.True(result.CanComplete);
    }

    [Theory]
    [InlineData(false, true, RegistrationValidationCodes.PersonalDataConsentRequired)]
    [InlineData(true, false, RegistrationValidationCodes.RegistrationConfirmationRequired)]
    public void ConsentAndFinalConfirmationAreRequired(
        bool personalDataConsent,
        bool registrationConfirmed,
        string expectedCode)
    {
        var draft = CompleteDraft() with
        {
            PersonalDataConsent = personalDataConsent,
            RegistrationConfirmed = registrationConfirmed
        };

        var result = RegistrationCompletionValidator.Evaluate(draft);

        Assert.False(result.CanComplete);
        Assert.Contains(result.Issues, issue => issue.Code == expectedCode);
    }

    public static TheoryData<string, Func<RegistrationDraft, RegistrationDraft>> RequiredFieldCases()
    {
        return new TheoryData<string, Func<RegistrationDraft, RegistrationDraft>>
        {
            { RegistrationFieldNames.FirstName, draft => draft with { FirstName = RegistrationField<string>.Missing() } },
            { RegistrationFieldNames.LastName, draft => draft with { LastName = RegistrationField<string>.Missing() } },
            { RegistrationFieldNames.DateOfBirth, draft => draft with { DateOfBirth = RegistrationField<DateOnly>.Missing() } },
            { RegistrationFieldNames.PhoneNumber, draft => draft with { PhoneNumber = RegistrationField<string>.Missing() } },
            { RegistrationFieldNames.CurrentRegion, draft => draft with { CurrentRegion = RegistrationField<string>.Missing() } },
            { RegistrationFieldNames.CurrentCity, draft => draft with { CurrentCity = RegistrationField<string>.Missing() } },
            { RegistrationFieldNames.UserCategory, draft => draft with { UserCategory = RegistrationField<UserCategory>.Missing() } }
        };
    }

    public static TheoryData<string, Func<RegistrationDraft, RegistrationDraft>> ConservativeConfirmationFieldCases()
    {
        return new TheoryData<string, Func<RegistrationDraft, RegistrationDraft>>
        {
            { RegistrationFieldNames.PhoneNumber, draft => draft with { PhoneNumber = RegistrationField<string>.Captured("+380501112233") } },
            { RegistrationFieldNames.DateOfBirth, draft => draft with { DateOfBirth = RegistrationField<DateOnly>.Captured(new DateOnly(1991, 8, 24)) } },
            { RegistrationFieldNames.CurrentRegion, draft => draft with { CurrentRegion = RegistrationField<string>.Captured("Kharkiv region") } },
            { RegistrationFieldNames.CurrentCity, draft => draft with { CurrentCity = RegistrationField<string>.Captured("Kharkiv") } },
            { RegistrationFieldNames.UserCategory, draft => draft with { UserCategory = RegistrationField<UserCategory>.Captured(UserCategory.Other) } }
        };
    }

    private static RegistrationDraft CompleteDraft()
    {
        return RegistrationDraft.Create() with
        {
            FirstName = RegistrationField<string>.Captured("Dimas"),
            LastName = RegistrationField<string>.Captured("Tkachenko"),
            DateOfBirth = RegistrationField<DateOnly>.Confirmed(new DateOnly(1991, 8, 24)),
            PhoneNumber = RegistrationField<string>.Confirmed("+380501112233"),
            CurrentRegion = RegistrationField<string>.Confirmed("Kharkiv region"),
            CurrentCity = RegistrationField<string>.Confirmed("Kharkiv"),
            UserCategory = RegistrationField<UserCategory>.Confirmed(UserCategory.Other),
            PersonalDataConsent = true,
            RegistrationConfirmed = true
        };
    }
}
