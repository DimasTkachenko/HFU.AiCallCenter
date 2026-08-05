using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Domain.Conversations;

namespace Hfu.VoiceRegistration.Application.Persistence;

public sealed record CompletedRegistrationRecord(
    Guid Id,
    Guid SessionId,
    string DemoRegistrationId,
    string FirstName,
    string LastName,
    string? Patronymic,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string? Email,
    string CurrentRegion,
    string? CurrentRegionReferenceId,
    string CurrentCity,
    string? ActualAddress,
    string UserCategory,
    string? RegionBeforeWar,
    string? RegionBeforeWarReferenceId,
    int? DisplacedCertificateYear,
    bool PersonalDataConsent,
    bool RegistrationConfirmed,
    DateTimeOffset CompletedAt);

public interface IRegistrationRepository
{
    Task SaveCompletedRegistrationAsync(
        Guid sessionId,
        FinalRegistrationDto finalRegistration,
        RegistrationResult registrationResult,
        CancellationToken cancellationToken = default);

    Task SaveSessionRecordAsync(
        ConversationSession session,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompletedRegistrationRecord>> GetCompletedRegistrationsAsync(
        CancellationToken cancellationToken = default);
}
