namespace Hfu.VoiceRegistration.Domain.Registration;

public static class RegistrationCompletionValidator
{
    public static RegistrationValidationResult Evaluate(RegistrationDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var issues = new List<RegistrationValidationIssue>();

        AddRequiredFieldIssue(issues, RegistrationFieldNames.FirstName, draft.FirstName);
        AddRequiredFieldIssue(issues, RegistrationFieldNames.LastName, draft.LastName);
        AddRequiredFieldIssue(issues, RegistrationFieldNames.DateOfBirth, draft.DateOfBirth);
        AddRequiredFieldIssue(issues, RegistrationFieldNames.PhoneNumber, draft.PhoneNumber);
        AddRequiredFieldIssue(issues, RegistrationFieldNames.CurrentRegion, draft.CurrentRegion);
        AddRequiredFieldIssue(issues, RegistrationFieldNames.CurrentCity, draft.CurrentCity);
        AddRequiredFieldIssue(issues, RegistrationFieldNames.UserCategory, draft.UserCategory);

        if (draft.UserCategory.Value == UserCategory.InternallyDisplacedPerson)
        {
            AddRequiredFieldIssue(issues, RegistrationFieldNames.RegionBeforeWar, draft.RegionBeforeWar);
            AddRequiredFieldIssue(
                issues,
                RegistrationFieldNames.DisplacedCertificateYear,
                draft.DisplacedCertificateYear);
        }

        AddConfirmationIssue(issues, RegistrationFieldNames.PhoneNumber, draft.PhoneNumber);
        AddConfirmationIssue(issues, RegistrationFieldNames.DateOfBirth, draft.DateOfBirth);
        AddConfirmationIssue(issues, RegistrationFieldNames.CurrentRegion, draft.CurrentRegion);
        AddConfirmationIssue(issues, RegistrationFieldNames.CurrentCity, draft.CurrentCity);
        AddConfirmationIssue(issues, RegistrationFieldNames.UserCategory, draft.UserCategory);
        AddOptionalEmailIssue(issues, draft.Email);

        if (!draft.PersonalDataConsent)
        {
            issues.Add(new RegistrationValidationIssue(
                RegistrationValidationCodes.PersonalDataConsentRequired,
                RegistrationFieldNames.PersonalDataConsent,
                "Personal data consent is required before registration can be completed."));
        }

        if (!draft.RegistrationConfirmed)
        {
            issues.Add(new RegistrationValidationIssue(
                RegistrationValidationCodes.RegistrationConfirmationRequired,
                RegistrationFieldNames.RegistrationConfirmed,
                "Final voice confirmation is required before registration can be completed."));
        }

        return new RegistrationValidationResult(issues);
    }

    private static void AddRequiredFieldIssue<T>(
        ICollection<RegistrationValidationIssue> issues,
        string fieldName,
        RegistrationField<T> field)
    {
        if (field.Status == RegistrationFieldStatus.Rejected)
        {
            issues.Add(new RegistrationValidationIssue(
                RegistrationValidationCodes.RequiredFieldRejected,
                fieldName,
                "Required field was rejected by the user."));
            return;
        }

        if (field.Status == RegistrationFieldStatus.NeedsClarification)
        {
            issues.Add(new RegistrationValidationIssue(
                RegistrationValidationCodes.NeedsClarification,
                fieldName,
                "Required field needs clarification."));
            return;
        }

        if (field.Status == RegistrationFieldStatus.Missing || !HasFieldValue(field))
        {
            issues.Add(new RegistrationValidationIssue(
                RegistrationValidationCodes.RequiredFieldMissing,
                fieldName,
                "Required field is missing."));
        }
    }

    private static void AddConfirmationIssue<T>(
        ICollection<RegistrationValidationIssue> issues,
        string fieldName,
        RegistrationField<T> field)
    {
        if (field.Status is RegistrationFieldStatus.Missing
            or RegistrationFieldStatus.NeedsClarification
            or RegistrationFieldStatus.Rejected)
        {
            return;
        }

        if (HasFieldValue(field) && field.Status != RegistrationFieldStatus.Confirmed)
        {
            issues.Add(new RegistrationValidationIssue(
                RegistrationValidationCodes.RequiresConfirmation,
                fieldName,
                "Field requires explicit confirmation."));
        }
    }

    private static void AddOptionalEmailIssue(
        ICollection<RegistrationValidationIssue> issues,
        RegistrationField<string> email)
    {
        if (email.Status is RegistrationFieldStatus.Missing or RegistrationFieldStatus.Rejected)
        {
            return;
        }

        if (email.Status == RegistrationFieldStatus.NeedsClarification)
        {
            issues.Add(new RegistrationValidationIssue(
                RegistrationValidationCodes.NeedsClarification,
                RegistrationFieldNames.Email,
                "Email needs clarification."));
            return;
        }

        if (HasFieldValue(email) && email.Status != RegistrationFieldStatus.Confirmed)
        {
            issues.Add(new RegistrationValidationIssue(
                RegistrationValidationCodes.RequiresConfirmation,
                RegistrationFieldNames.Email,
                "Email requires explicit confirmation when provided."));
        }
    }

    private static bool HasFieldValue<T>(RegistrationField<T> field)
    {
        return field.Value switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => true
        };
    }
}
