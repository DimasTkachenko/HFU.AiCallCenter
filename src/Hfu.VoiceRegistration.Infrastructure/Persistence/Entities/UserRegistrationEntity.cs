namespace Hfu.VoiceRegistration.Infrastructure.Persistence.Entities;

public sealed class UserRegistrationEntity
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string DemoRegistrationId { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Patronymic { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string CurrentRegion { get; set; } = string.Empty;

    public string? CurrentRegionReferenceId { get; set; }

    public string CurrentCity { get; set; } = string.Empty;

    public string? ActualAddress { get; set; }

    public string UserCategory { get; set; } = string.Empty;

    public string? RegionBeforeWar { get; set; }

    public string? RegionBeforeWarReferenceId { get; set; }

    public int? DisplacedCertificateYear { get; set; }

    public bool PersonalDataConsent { get; set; }

    public bool RegistrationConfirmed { get; set; }

    public DateTimeOffset CompletedAt { get; set; }
}
