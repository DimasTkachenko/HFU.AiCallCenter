using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Application.RegistrationTools;
using Hfu.VoiceRegistration.Domain.Conversations;
using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Application.Tests.RegistrationTools;

public sealed class RegistrationToolServiceTests
{
    [Fact]
    public async Task UpdateRegistrationFieldsCapturesNormalizedTypedValuesAndReturnsCurrentState()
    {
        var createdAt = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var activityAt = createdAt.AddMinutes(1);
        var store = new FakeConversationSessionStore();
        var session = ConversationSession.Create(createdAt);
        await store.CreateAsync(session, CancellationToken.None);
        var service = CreateService(store, activityAt);

        var result = await service.UpdateRegistrationFieldsAsync(
            session.SessionId,
            new[]
            {
                new RegistrationFieldUpdate(RegistrationFieldNames.FirstName, " Dimas ", " Dimas "),
                new RegistrationFieldUpdate(RegistrationFieldNames.DateOfBirth, "1991-08-24", "twenty fourth August"),
                new RegistrationFieldUpdate(RegistrationFieldNames.UserCategory, "other", "other"),
                new RegistrationFieldUpdate(RegistrationFieldNames.PersonalDataConsent, true, "yes")
            },
            CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.True(result.Succeeded, FormatErrors(result));
        Assert.NotNull(stored);
        Assert.Equal("Dimas", stored.RegistrationDraft.FirstName.Value);
        Assert.Equal(" Dimas ", stored.RegistrationDraft.FirstName.RawValue);
        Assert.Equal(RegistrationFieldStatus.Captured, stored.RegistrationDraft.FirstName.Status);
        Assert.Equal(new DateOnly(1991, 8, 24), stored.RegistrationDraft.DateOfBirth.Value);
        Assert.Equal(UserCategory.Other, stored.RegistrationDraft.UserCategory.Value);
        Assert.True(stored.RegistrationDraft.PersonalDataConsent);
        Assert.Equal(1, stored.Version);
        Assert.Equal(activityAt, stored.LastActivityAt);
        Assert.NotNull(result.State);
        Assert.Contains(
            result.State.Fields,
            field => field.Name == RegistrationFieldNames.FirstName
                && Equals("Dimas", field.Value)
                && field.Status == RegistrationFieldStatus.Captured);
    }

    [Fact]
    public async Task UpdateRegistrationFieldsRejectsUnknownAndInvalidValuesWithoutChangingDraft()
    {
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var store = new FakeConversationSessionStore();
        var session = ConversationSession.Create(now);
        await store.CreateAsync(session, CancellationToken.None);
        var service = CreateService(store, now.AddMinutes(1));

        var result = await service.UpdateRegistrationFieldsAsync(
            session.SessionId,
            new[]
            {
                new RegistrationFieldUpdate("unknownField", "value"),
                new RegistrationFieldUpdate(RegistrationFieldNames.DateOfBirth, "not-a-date"),
                new RegistrationFieldUpdate(RegistrationFieldNames.DisplacedCertificateYear, "2013")
            },
            CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == RegistrationToolErrorCodes.UnknownField);
        Assert.Contains(result.Errors, error => error.Code == RegistrationToolErrorCodes.InvalidFieldValue);
        Assert.NotNull(stored);
        Assert.Equal(0, stored.Version);
        Assert.Equal(RegistrationFieldStatus.Missing, stored.RegistrationDraft.DateOfBirth.Status);
        Assert.Equal(RegistrationFieldStatus.Missing, stored.RegistrationDraft.DisplacedCertificateYear.Status);
    }

    [Fact]
    public async Task UpdateRegistrationFieldsResolvesRegionAliasesToUkrainianCanonicalNames()
    {
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var store = new FakeConversationSessionStore();
        var session = ConversationSession.Create(now);
        await store.CreateAsync(session, CancellationToken.None);
        var service = CreateService(store, now.AddMinutes(1));

        var result = await service.UpdateRegistrationFieldsAsync(
            session.SessionId,
            new[]
            {
                new RegistrationFieldUpdate(
                    RegistrationFieldNames.CurrentRegion,
                    "Харьковская область",
                    "Харьковская область"),
                new RegistrationFieldUpdate(
                    RegistrationFieldNames.RegionBeforeWar,
                    "Харківська",
                    "Харківська")
            },
            CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.True(result.Succeeded, FormatErrors(result));
        Assert.NotNull(stored);
        Assert.Equal("Харківська область", stored.RegistrationDraft.CurrentRegion.Value);
        Assert.Equal("hfu-region-kharkivska", stored.RegistrationDraft.CurrentRegion.ReferenceId);
        Assert.Equal(RegistrationFieldStatus.Captured, stored.RegistrationDraft.CurrentRegion.Status);
        Assert.Equal("Харківська область", stored.RegistrationDraft.RegionBeforeWar.Value);
        Assert.Equal("hfu-region-kharkivska", stored.RegistrationDraft.RegionBeforeWar.ReferenceId);
    }

    [Fact]
    public async Task UpdateRegistrationFieldsMarksAmbiguousRegionForClarificationWithSuggestions()
    {
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var store = new FakeConversationSessionStore();
        var session = ConversationSession.Create(now);
        await store.CreateAsync(session, CancellationToken.None);
        var service = CreateService(store, now.AddMinutes(1));

        var result = await service.UpdateRegistrationFieldsAsync(
            session.SessionId,
            new[]
            {
                new RegistrationFieldUpdate(RegistrationFieldNames.CurrentRegion, "Київ", "Київ")
            },
            CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == RegistrationToolErrorCodes.RegionAmbiguous);
        Assert.Contains(
            result.Errors.SelectMany(error => error.Suggestions),
            suggestion => suggestion == "Київська область");
        Assert.Contains(
            result.Errors.SelectMany(error => error.Suggestions),
            suggestion => suggestion == "м. Київ");
        Assert.NotNull(stored);
        Assert.Equal(1, stored.Version);
        Assert.Equal(RegistrationFieldStatus.NeedsClarification, stored.RegistrationDraft.CurrentRegion.Status);
        Assert.Null(stored.RegistrationDraft.CurrentRegion.Value);
        Assert.Equal("Київ", stored.RegistrationDraft.CurrentRegion.RawValue);
        Assert.Contains("ambiguous", stored.RegistrationDraft.CurrentRegion.ClarificationReason);
        Assert.NotNull(result.State);
        Assert.Contains(
            result.State.FieldsRequiringClarification,
            field => field == RegistrationFieldNames.CurrentRegion);
    }

    [Fact]
    public async Task UpdateRegistrationFieldsMarksUnknownRegionForClarificationWithoutAcceptingIds()
    {
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var store = new FakeConversationSessionStore();
        var session = ConversationSession.Create(now);
        await store.CreateAsync(session, CancellationToken.None);
        var service = CreateService(store, now.AddMinutes(1));

        var result = await service.UpdateRegistrationFieldsAsync(
            session.SessionId,
            new[]
            {
                new RegistrationFieldUpdate(
                    RegistrationFieldNames.CurrentRegion,
                    "hfu-region-kharkivska",
                    "hfu-region-kharkivska")
            },
            CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == RegistrationToolErrorCodes.RegionNotFound);
        Assert.NotNull(stored);
        Assert.Equal(RegistrationFieldStatus.NeedsClarification, stored.RegistrationDraft.CurrentRegion.Status);
        Assert.Null(stored.RegistrationDraft.CurrentRegion.Value);
        Assert.Equal("hfu-region-kharkivska", stored.RegistrationDraft.CurrentRegion.RawValue);
    }

    [Fact]
    public async Task ConfirmRegistrationFieldsConfirmsCapturedFields()
    {
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var store = new FakeConversationSessionStore();
        var session = ConversationSession.Create(now) with
        {
            RegistrationDraft = RegistrationDraft.Create() with
            {
                PhoneNumber = RegistrationField<string>.Captured("+380501112233")
            }
        };
        await store.CreateAsync(session, CancellationToken.None);
        var service = CreateService(store, now.AddMinutes(1));

        var result = await service.ConfirmRegistrationFieldsAsync(
            session.SessionId,
            new[] { RegistrationFieldNames.PhoneNumber },
            CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.True(result.Succeeded, FormatErrors(result));
        Assert.NotNull(stored);
        Assert.Equal(RegistrationFieldStatus.Confirmed, stored.RegistrationDraft.PhoneNumber.Status);
    }

    [Fact]
    public async Task ConfirmRegistrationFieldsRejectsMissingFields()
    {
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var store = new FakeConversationSessionStore();
        var session = ConversationSession.Create(now);
        await store.CreateAsync(session, CancellationToken.None);
        var service = CreateService(store, now.AddMinutes(1));

        var result = await service.ConfirmRegistrationFieldsAsync(
            session.SessionId,
            new[] { RegistrationFieldNames.PhoneNumber },
            CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == RegistrationToolErrorCodes.FieldCannotBeConfirmed);
        Assert.NotNull(stored);
        Assert.Equal(0, stored.Version);
    }

    [Fact]
    public async Task MarkFieldsForClarificationPreservesValueAndStoresReason()
    {
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var store = new FakeConversationSessionStore();
        var session = ConversationSession.Create(now) with
        {
            RegistrationDraft = RegistrationDraft.Create() with
            {
                PhoneNumber = RegistrationField<string>.Captured("+380501112233", "050 111 22 33")
            }
        };
        await store.CreateAsync(session, CancellationToken.None);
        var service = CreateService(store, now.AddMinutes(1));

        var result = await service.MarkFieldsForClarificationAsync(
            session.SessionId,
            new[] { RegistrationFieldNames.PhoneNumber },
            "Last two digits were unclear.",
            CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.True(result.Succeeded, FormatErrors(result));
        Assert.NotNull(stored);
        Assert.Equal(RegistrationFieldStatus.NeedsClarification, stored.RegistrationDraft.PhoneNumber.Status);
        Assert.Equal("+380501112233", stored.RegistrationDraft.PhoneNumber.Value);
        Assert.Equal("Last two digits were unclear.", stored.RegistrationDraft.PhoneNumber.ClarificationReason);
        Assert.NotNull(result.State);
        Assert.Contains(result.State.FieldsRequiringClarification, field => field == RegistrationFieldNames.PhoneNumber);
    }

    [Fact]
    public async Task ClearRegistrationFieldsReturnsFieldsToMissing()
    {
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var store = new FakeConversationSessionStore();
        var session = ConversationSession.Create(now) with
        {
            RegistrationDraft = RegistrationDraft.Create() with
            {
                PhoneNumber = RegistrationField<string>.Confirmed("+380501112233"),
                PersonalDataConsent = true
            }
        };
        await store.CreateAsync(session, CancellationToken.None);
        var service = CreateService(store, now.AddMinutes(1));

        var result = await service.ClearRegistrationFieldsAsync(
            session.SessionId,
            new[] { RegistrationFieldNames.PhoneNumber, RegistrationFieldNames.PersonalDataConsent },
            CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.True(result.Succeeded, FormatErrors(result));
        Assert.NotNull(stored);
        Assert.Equal(RegistrationFieldStatus.Missing, stored.RegistrationDraft.PhoneNumber.Status);
        Assert.False(stored.RegistrationDraft.PersonalDataConsent);
    }

    [Fact]
    public async Task GetRegistrationStateReportsMissingClarificationConfirmationAndCompletionStatus()
    {
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var store = new FakeConversationSessionStore();
        var session = ConversationSession.Create(now) with
        {
            RegistrationDraft = RegistrationDraft.Create() with
            {
                PhoneNumber = RegistrationField<string>.NeedsClarification("+380501112233"),
                Email = RegistrationField<string>.Captured("person@example.com")
            }
        };
        await store.CreateAsync(session, CancellationToken.None);
        var service = CreateService(store, now);

        var result = await service.GetRegistrationStateAsync(session.SessionId, CancellationToken.None);

        Assert.True(result.Succeeded, FormatErrors(result));
        Assert.NotNull(result.State);
        Assert.Contains(result.State.MissingRequiredFields, field => field == RegistrationFieldNames.FirstName);
        Assert.Contains(result.State.FieldsRequiringClarification, field => field == RegistrationFieldNames.PhoneNumber);
        Assert.Contains(result.State.FieldsAwaitingConfirmation, field => field == RegistrationFieldNames.Email);
        Assert.False(result.State.RegistrationCanBeCompleted);
    }

    [Fact]
    public async Task GetRegistrationStateReturnsSessionNotFoundErrorForUnknownSession()
    {
        var store = new FakeConversationSessionStore();
        var service = CreateService(store, new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero));

        var result = await service.GetRegistrationStateAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(result.State);
        Assert.Contains(result.Errors, error => error.Code == RegistrationToolErrorCodes.SessionNotFound);
    }

    private static RegistrationToolService CreateService(
        IConversationSessionStore store,
        DateTimeOffset utcNow)
    {
        return new RegistrationToolService(store, new FakeTimeProvider(utcNow));
    }

    private static string FormatErrors(RegistrationToolResult result)
    {
        return string.Join("; ", result.Errors.Select(error => $"{error.Code}:{error.Field}:{error.Message}"));
    }

    private sealed class FakeConversationSessionStore : IConversationSessionStore
    {
        private readonly Dictionary<Guid, ConversationSession> _sessions = new();

        public Task<ConversationSession?> GetAsync(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        public Task CreateAsync(
            ConversationSession session,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessions.Add(session.SessionId, session);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            ConversationSession session,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessions[session.SessionId] = session;
            return Task.CompletedTask;
        }

        public async Task<ConversationSession> UpdateAsync(
            Guid sessionId,
            Func<ConversationSession, CancellationToken, Task<ConversationSession>> mutate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_sessions.TryGetValue(sessionId, out var current))
            {
                throw new KeyNotFoundException($"Conversation session '{sessionId}' was not found.");
            }

            var mutated = await mutate(current, cancellationToken);
            if (mutated.Version <= current.Version)
            {
                mutated = mutated with { Version = current.Version + 1 };
            }

            _sessions[sessionId] = mutated;
            return mutated;
        }

        public Task RemoveAsync(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessions.Remove(sessionId);
            return Task.CompletedTask;
        }

        public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(0);
        }
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
