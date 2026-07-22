namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public static class RegistrationToolErrorCodes
{
    public const string SessionNotFound = "SessionNotFound";
    public const string EmptyFieldList = "EmptyFieldList";
    public const string UnknownField = "UnknownField";
    public const string InvalidFieldValue = "InvalidFieldValue";
    public const string RegionAmbiguous = "RegionAmbiguous";
    public const string RegionNotFound = "RegionNotFound";
    public const string FieldCannotBeConfirmed = "FieldCannotBeConfirmed";
    public const string UnsupportedFieldOperation = "UnsupportedFieldOperation";
    public const string RegistrationCannotBeCompleted = "RegistrationCannotBeCompleted";
    public const string RegistrationAlreadyCompleted = "RegistrationAlreadyCompleted";
}
