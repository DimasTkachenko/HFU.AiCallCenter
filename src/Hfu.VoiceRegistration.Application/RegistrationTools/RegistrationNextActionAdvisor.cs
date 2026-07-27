using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Application.RegistrationTools;

internal static class RegistrationNextActionAdvisor
{
    private static readonly string[] RegistrationFieldPriority =
    [
        RegistrationFieldNames.FirstName,
        RegistrationFieldNames.LastName,
        RegistrationFieldNames.DateOfBirth,
        RegistrationFieldNames.PhoneNumber,
        RegistrationFieldNames.CurrentRegion,
        RegistrationFieldNames.CurrentCity,
        RegistrationFieldNames.UserCategory,
        RegistrationFieldNames.RegionBeforeWar,
        RegistrationFieldNames.DisplacedCertificateYear
    ];

    public static RegistrationRecommendedNextAction? Recommend(
        RegistrationStateSnapshot? state,
        RegistrationCompletionDetails? completion)
    {
        if (completion is not null)
        {
            return new RegistrationRecommendedNextAction(
                "Completed",
                null,
                "Tell the user in Ukrainian that the demo registration is completed and provide the registration id when available.");
        }

        if (state is null)
        {
            return null;
        }

        var clarificationField = FirstByPriority(state.FieldsRequiringClarification);
        if (clarificationField is not null)
        {
            return new RegistrationRecommendedNextAction(
                "ClarifyField",
                clarificationField,
                $"Ask the user to clarify {clarificationField}. Use backend errors or suggestions if they are present.");
        }

        var missingRegistrationField = FirstByPriority(state.MissingRequiredFields);
        if (missingRegistrationField is not null)
        {
            return new RegistrationRecommendedNextAction(
                "AskField",
                missingRegistrationField,
                $"Ask the user for {missingRegistrationField}. Ask one short question in Ukrainian.");
        }

        var confirmationField = FirstByPriority(state.FieldsAwaitingConfirmation);
        if (confirmationField is not null)
        {
            return new RegistrationRecommendedNextAction(
                "ConfirmField",
                confirmationField,
                $"Read back {confirmationField} and ask the user to confirm it explicitly.");
        }

        if (state.MissingRequiredFields.Contains(RegistrationFieldNames.PersonalDataConsent, StringComparer.Ordinal))
        {
            return new RegistrationRecommendedNextAction(
                "AskPersonalDataConsent",
                RegistrationFieldNames.PersonalDataConsent,
                "Give a short final summary first, then ask for explicit consent to process the provided personal data for this demo registration.");
        }

        if (state.MissingRequiredFields.Contains(RegistrationFieldNames.RegistrationConfirmed, StringComparer.Ordinal))
        {
            return new RegistrationRecommendedNextAction(
                "AskFinalRegistrationConfirmation",
                RegistrationFieldNames.RegistrationConfirmed,
                "Ask the user for explicit final confirmation to complete the demo registration.");
        }

        return state.RegistrationCanBeCompleted
            ? new RegistrationRecommendedNextAction(
                "CompleteRegistration",
                null,
                "Call complete_registration with personalDataConsent=true and registrationConfirmed=true.")
            : new RegistrationRecommendedNextAction(
                "GetRegistrationState",
                null,
                "Call get_registration_state before deciding the next interview step.");
    }

    private static string? FirstByPriority(IReadOnlyCollection<string> fieldNames)
    {
        foreach (var fieldName in RegistrationFieldPriority)
        {
            if (fieldNames.Contains(fieldName, StringComparer.Ordinal))
            {
                return fieldName;
            }
        }

        return fieldNames.FirstOrDefault(fieldName =>
            !string.Equals(fieldName, RegistrationFieldNames.PersonalDataConsent, StringComparison.Ordinal)
            && !string.Equals(fieldName, RegistrationFieldNames.RegistrationConfirmed, StringComparison.Ordinal));
    }
}
