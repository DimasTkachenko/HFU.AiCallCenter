using System.Text.Json;
using Hfu.VoiceRegistration.Application.Persistence;
using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Domain.Conversations;
using Hfu.VoiceRegistration.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hfu.VoiceRegistration.Infrastructure.Persistence;

public sealed class PostgresRegistrationRepository : IRegistrationRepository
{
    private readonly VoiceRegistrationDbContext _dbContext;
    private readonly ILogger<PostgresRegistrationRepository>? _logger;

    public PostgresRegistrationRepository(
        VoiceRegistrationDbContext dbContext,
        ILogger<PostgresRegistrationRepository>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task SaveCompletedRegistrationAsync(
        Guid sessionId,
        FinalRegistrationDto finalRegistration,
        RegistrationResult registrationResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(finalRegistration);
        ArgumentNullException.ThrowIfNull(registrationResult);

        try
        {
            var existing = await _dbContext.UserRegistrations
                .FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);

            if (existing is null)
            {
                var demoId = registrationResult.RegistrationId;
                var duplicateDemoId = await _dbContext.UserRegistrations
                    .AnyAsync(x => x.DemoRegistrationId == demoId, cancellationToken);

                if (duplicateDemoId)
                {
                    demoId = $"{registrationResult.RegistrationId}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
                }

                var entity = new UserRegistrationEntity
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    DemoRegistrationId = demoId,
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
                    CompletedAt = registrationResult.CompletedAt.ToUniversalTime()
                };

                _dbContext.UserRegistrations.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger?.LogInformation("Successfully saved completed user registration for session {SessionId} (DemoId: {DemoId}) to PostgreSQL DB.", sessionId, demoId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save completed registration for session {SessionId} to PostgreSQL DB.", sessionId);
            throw;
        }
    }

    public async Task SaveSessionRecordAsync(
        ConversationSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
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
                    CreatedAt = session.CreatedAt.ToUniversalTime(),
                    LastActivityAt = session.LastActivityAt.ToUniversalTime(),
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
                existing.LastActivityAt = session.LastActivityAt.ToUniversalTime();
                existing.EventCount = session.Events.Count;
                existing.DraftJson = draftJson;
                existing.EventsJson = eventsJson;
                existing.DemoRegistrationId = session.RegistrationResult?.RegistrationId;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger?.LogInformation("Successfully saved session audit record for session {SessionId} to PostgreSQL DB.", session.SessionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save session record for session {SessionId} to PostgreSQL DB.", session.SessionId);
            throw;
        }
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
