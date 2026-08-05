using System.Text.Json;
using Hfu.VoiceRegistration.Application.Persistence;
using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Domain.Conversations;
using Hfu.VoiceRegistration.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hfu.VoiceRegistration.Infrastructure.Persistence;

public sealed class PostgresRegistrationRepository : IRegistrationRepository
{
    private readonly VoiceRegistrationDbContext _dbContext;

    public PostgresRegistrationRepository(VoiceRegistrationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task SaveCompletedRegistrationAsync(
        Guid sessionId,
        FinalRegistrationDto finalRegistration,
        RegistrationResult registrationResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(finalRegistration);
        ArgumentNullException.ThrowIfNull(registrationResult);

        var existing = await _dbContext.UserRegistrations
            .FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);

        if (existing is null)
        {
            var entity = new UserRegistrationEntity
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                DemoRegistrationId = registrationResult.RegistrationId,
                FirstName = finalRegistration.FirstName,
                LastName = finalRegistration.LastName,
                Patronymic = finalRegistration.Patronymic,
                DateOfBirth = finalRegistration.DateOfBirth,
                PhoneNumber = finalRegistration.PhoneNumber,
                Email = finalRegistration.Email,
                CurrentRegion = finalRegistration.CurrentRegion,
                CurrentRegionReferenceId = finalRegistration.CurrentRegionReferenceId,
                CurrentCity = finalRegistration.CurrentCity,
                ActualAddress = finalRegistration.ActualAddress,
                UserCategory = finalRegistration.UserCategory.ToString(),
                RegionBeforeWar = finalRegistration.RegionBeforeWar,
                RegionBeforeWarReferenceId = finalRegistration.RegionBeforeWarReferenceId,
                DisplacedCertificateYear = finalRegistration.DisplacedCertificateYear,
                PersonalDataConsent = finalRegistration.PersonalDataConsent,
                RegistrationConfirmed = finalRegistration.RegistrationConfirmed,
                CompletedAt = registrationResult.CompletedAt
            };

            _dbContext.UserRegistrations.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SaveSessionRecordAsync(
        ConversationSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var draftJson = JsonSerializer.Serialize(session.RegistrationDraft);
        var eventsJson = JsonSerializer.Serialize(session.Events);

        var existing = await _dbContext.SessionAuditLogs
            .FirstOrDefaultAsync(x => x.SessionId == session.SessionId, cancellationToken);

        if (existing is null)
        {
            var record = new SessionRecordEntity
            {
                SessionId = session.SessionId,
                Status = session.Status.ToString(),
                CreatedAt = session.CreatedAt,
                LastActivityAt = session.LastActivityAt,
                EventCount = session.Events.Count,
                DraftJson = draftJson,
                EventsJson = eventsJson,
                DemoRegistrationId = session.RegistrationResult?.RegistrationId
            };

            _dbContext.SessionAuditLogs.Add(record);
        }
        else
        {
            existing.Status = session.Status.ToString();
            existing.LastActivityAt = session.LastActivityAt;
            existing.EventCount = session.Events.Count;
            existing.DraftJson = draftJson;
            existing.EventsJson = eventsJson;
            existing.DemoRegistrationId = session.RegistrationResult?.RegistrationId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CompletedRegistrationRecord>> GetCompletedRegistrationsAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.UserRegistrations
            .AsNoTracking()
            .OrderByDescending(x => x.CompletedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => new CompletedRegistrationRecord(
            e.Id,
            e.SessionId,
            e.DemoRegistrationId,
            e.FirstName,
            e.LastName,
            e.Patronymic,
            e.DateOfBirth,
            e.PhoneNumber,
            e.Email,
            e.CurrentRegion,
            e.CurrentRegionReferenceId,
            e.CurrentCity,
            e.ActualAddress,
            e.UserCategory,
            e.RegionBeforeWar,
            e.RegionBeforeWarReferenceId,
            e.DisplacedCertificateYear,
            e.PersonalDataConsent,
            e.RegistrationConfirmed,
            e.CompletedAt
        )).ToList();
    }
}
