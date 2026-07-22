namespace Hfu.VoiceRegistration.Domain.Registration;

public sealed record RegistrationField<T>
{
    private RegistrationField(
        T? value,
        string? rawValue,
        RegistrationFieldStatus status,
        string? clarificationReason,
        string? referenceId)
    {
        Value = value;
        RawValue = rawValue;
        Status = status;
        ClarificationReason = clarificationReason;
        ReferenceId = referenceId;
    }

    public T? Value { get; }

    public string? RawValue { get; }

    public RegistrationFieldStatus Status { get; }

    public string? ClarificationReason { get; }

    public string? ReferenceId { get; }

    public bool HasValue => Value is not null;

    public static RegistrationField<T> Missing()
    {
        return new RegistrationField<T>(default, null, RegistrationFieldStatus.Missing, null, null);
    }

    public static RegistrationField<T> Captured(
        T value,
        string? rawValue = null,
        string? referenceId = null)
    {
        return new RegistrationField<T>(
            value,
            rawValue,
            RegistrationFieldStatus.Captured,
            null,
            referenceId);
    }

    public static RegistrationField<T> NeedsClarification(
        T? value = default,
        string? rawValue = null,
        string? clarificationReason = null,
        string? referenceId = null)
    {
        return new RegistrationField<T>(
            value,
            rawValue,
            RegistrationFieldStatus.NeedsClarification,
            clarificationReason,
            referenceId);
    }

    public static RegistrationField<T> Confirmed(
        T value,
        string? rawValue = null,
        string? referenceId = null)
    {
        return new RegistrationField<T>(
            value,
            rawValue,
            RegistrationFieldStatus.Confirmed,
            null,
            referenceId);
    }

    public static RegistrationField<T> Rejected()
    {
        return new RegistrationField<T>(default, null, RegistrationFieldStatus.Rejected, null, null);
    }
}
