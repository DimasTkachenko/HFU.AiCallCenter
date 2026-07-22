namespace Hfu.VoiceRegistration.Domain.Registration;

public sealed record RegistrationField<T>
{
    private RegistrationField(T? value, string? rawValue, RegistrationFieldStatus status)
    {
        Value = value;
        RawValue = rawValue;
        Status = status;
    }

    public T? Value { get; }

    public string? RawValue { get; }

    public RegistrationFieldStatus Status { get; }

    public bool HasValue => Value is not null;

    public static RegistrationField<T> Missing()
    {
        return new RegistrationField<T>(default, null, RegistrationFieldStatus.Missing);
    }

    public static RegistrationField<T> Captured(T value, string? rawValue = null)
    {
        return new RegistrationField<T>(value, rawValue, RegistrationFieldStatus.Captured);
    }

    public static RegistrationField<T> NeedsClarification(T? value = default, string? rawValue = null)
    {
        return new RegistrationField<T>(value, rawValue, RegistrationFieldStatus.NeedsClarification);
    }

    public static RegistrationField<T> Confirmed(T value, string? rawValue = null)
    {
        return new RegistrationField<T>(value, rawValue, RegistrationFieldStatus.Confirmed);
    }

    public static RegistrationField<T> Rejected()
    {
        return new RegistrationField<T>(default, null, RegistrationFieldStatus.Rejected);
    }
}
