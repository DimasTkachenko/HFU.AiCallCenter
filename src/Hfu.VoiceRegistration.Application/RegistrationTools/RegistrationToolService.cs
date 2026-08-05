using System.Globalization;
using System.Net.Mail;
using System.Text.Json;
using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Application.Persistence;
using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Application.ReferenceData;
using Hfu.VoiceRegistration.Domain.Conversations;
using Hfu.VoiceRegistration.Domain.Registration;

using Microsoft.Extensions.Logging;

namespace Hfu.VoiceRegistration.Application.RegistrationTools;

public sealed class RegistrationToolService : IRegistrationToolService
{
    private const string FieldUpdatedEventType = "FieldUpdated";
    private const string FieldConfirmedEventType = "FieldConfirmed";
    private const string FieldNeedsClarificationEventType = "FieldNeedsClarification";
    private const string FieldClearedEventType = "FieldCleared";
    private const string ValidationFailedEventType = "ValidationFailed";
    private const string RegistrationCompletingEventType = "RegistrationCompleting";
    private const string RegistrationCompletedEventType = "RegistrationCompleted";

    private readonly IConversationSessionStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly IRegionResolver _regionResolver;
    private readonly IFakeHfuRegistrationService? _fakeHfuRegistrationService;
    private readonly RegistrationFieldRegistry _fieldRegistry;
    private readonly IRegistrationRepository? _repository;
    private readonly ILogger<RegistrationToolService>? _logger;

    public RegistrationToolService(
        IConversationSessionStore store,
        TimeProvider timeProvider,
        IRegionResolver? regionResolver = null,
        IFakeHfuRegistrationService? fakeHfuRegistrationService = null,
        IRegistrationRepository? repository = null,
        ILogger<RegistrationToolService>? logger = null)
    {
        _store = store;
        _timeProvider = timeProvider;
        _regionResolver = regionResolver
            ?? new RegionResolver(new UkrainianRegionReferenceDataProvider());
        _fakeHfuRegistrationService = fakeHfuRegistrationService;
        _fieldRegistry = RegistrationFieldRegistry.Instance;
        _repository = repository;
        _logger = logger;
    }

    public async Task<RegistrationToolResult> UpdateRegistrationFieldsAsync(
        Guid sessionId,
        IReadOnlyCollection<RegistrationFieldUpdate> fields,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fields);

        if (fields.Count == 0)
        {
            return await FailureFromCurrentStateAsync(
                sessionId,
                new RegistrationToolError(
                    RegistrationToolErrorCodes.EmptyFieldList,
                    null,
                    "At least one field update is required."),
                cancellationToken);
        }

        return await MutateAsync(
            sessionId,
            current =>
            {
                var context = CreateConversionContext();
                var draft = current.RegistrationDraft;
                var hardErrors = new List<RegistrationToolError>();
                var handledErrors = new List<RegistrationToolError>();

                foreach (var update in fields)
                {
                    if (!_fieldRegistry.TryGet(update.Name, out var field))
                    {
                        hardErrors.Add(UnknownField(update.Name));
                        continue;
                    }

                    var updateResult = field.Update(draft, update, context);
                    if (updateResult.Error is not null)
                    {
                        if (updateResult.ShouldPersist)
                        {
                            handledErrors.Add(updateResult.Error);
                        }
                        else
                        {
                            hardErrors.Add(updateResult.Error);
                            continue;
                        }
                    }

                    draft = updateResult.Draft;
                }

                if (hardErrors.Count > 0)
                {
                    return ToolMutation.Invalid(current, hardErrors);
                }

                return handledErrors.Count > 0
                    ? ToolMutation.ValidWithErrors(
                        draft,
                        FieldNeedsClarificationEventType,
                        $"Updated {fields.Count} registration field(s) with clarification required.",
                        handledErrors)
                    : ToolMutation.Valid(draft, FieldUpdatedEventType, $"Updated {fields.Count} registration field(s).");
            },
            cancellationToken);
    }

    public async Task<RegistrationToolResult> ConfirmRegistrationFieldsAsync(
        Guid sessionId,
        IReadOnlyCollection<string> fieldNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fieldNames);

        if (fieldNames.Count == 0)
        {
            return await FailureFromCurrentStateAsync(
                sessionId,
                new RegistrationToolError(
                    RegistrationToolErrorCodes.EmptyFieldList,
                    null,
                    "At least one field name is required."),
                cancellationToken);
        }

        return await MutateFieldNamesAsync(
            sessionId,
            fieldNames,
            static (field, draft) => field.Confirm(draft),
            FieldConfirmedEventType,
            $"Confirmed {fieldNames.Count} registration field(s).",
            cancellationToken);
    }

    public async Task<RegistrationToolResult> MarkFieldsForClarificationAsync(
        Guid sessionId,
        IReadOnlyCollection<string> fieldNames,
        string? reason,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fieldNames);

        if (fieldNames.Count == 0)
        {
            return await FailureFromCurrentStateAsync(
                sessionId,
                new RegistrationToolError(
                    RegistrationToolErrorCodes.EmptyFieldList,
                    null,
                    "At least one field name is required."),
                cancellationToken);
        }

        return await MutateFieldNamesAsync(
            sessionId,
            fieldNames,
            (field, draft) => field.MarkNeedsClarification(draft, reason),
            FieldNeedsClarificationEventType,
            "Marked registration field(s) for clarification.",
            cancellationToken);
    }

    public async Task<RegistrationToolResult> ClearRegistrationFieldsAsync(
        Guid sessionId,
        IReadOnlyCollection<string> fieldNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fieldNames);

        if (fieldNames.Count == 0)
        {
            return await FailureFromCurrentStateAsync(
                sessionId,
                new RegistrationToolError(
                    RegistrationToolErrorCodes.EmptyFieldList,
                    null,
                    "At least one field name is required."),
                cancellationToken);
        }

        return await MutateFieldNamesAsync(
            sessionId,
            fieldNames,
            static (field, draft) => field.Clear(draft),
            FieldClearedEventType,
            $"Cleared {fieldNames.Count} registration field(s).",
            cancellationToken);
    }

    public async Task<RegistrationToolResult> GetRegistrationStateAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _store.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return RegistrationToolResult.Failure(
                null,
                new[]
                {
                    new RegistrationToolError(
                        RegistrationToolErrorCodes.SessionNotFound,
                        null,
                        $"Conversation session '{sessionId}' was not found.")
                });
        }

        return RegistrationToolResult.Success(CreateState(session));
    }

    public async Task<RegistrationToolResult> CompleteRegistrationAsync(
        Guid sessionId,
        CompleteRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentSession = await _store.GetAsync(sessionId, cancellationToken);
        if (currentSession is null)
        {
            return RegistrationToolResult.Failure(
                null,
                new[]
                {
                    new RegistrationToolError(
                        RegistrationToolErrorCodes.SessionNotFound,
                        null,
                        $"Conversation session '{sessionId}' was not found.")
                });
        }

        if (currentSession.Status == ConversationSessionStatus.Completed
            && currentSession.RegistrationResult is not null)
        {
            return RegistrationToolResult.Failure(
                CreateState(currentSession),
                new[]
                {
                    new RegistrationToolError(
                        RegistrationToolErrorCodes.RegistrationAlreadyCompleted,
                        null,
                        "Registration has already been completed.")
                },
                CreateCompletionDetails(currentSession));
        }

        IReadOnlyList<RegistrationToolError> errors = Array.Empty<RegistrationToolError>();
        RegistrationCompletionDetails? completion = null;

        ConversationSession updated;
        try
        {
            updated = await _store.UpdateAsync(
                sessionId,
                async (current, mutationCancellationToken) =>
                {
                    if (current.Status == ConversationSessionStatus.Completed
                        && current.RegistrationResult is not null)
                    {
                        errors = new[]
                        {
                            new RegistrationToolError(
                                RegistrationToolErrorCodes.RegistrationAlreadyCompleted,
                                null,
                                "Registration has already been completed.")
                        };
                        completion = CreateCompletionDetails(current);
                        return current;
                    }

                    var now = _timeProvider.GetUtcNow();
                    var draft = current.RegistrationDraft with
                    {
                        PersonalDataConsent = request.PersonalDataConsent,
                        RegistrationConfirmed = request.RegistrationConfirmed
                    };
                    var validation = RegistrationCompletionValidator.Evaluate(draft);
                    if (!validation.CanComplete)
                    {
                        errors = new[]
                        {
                            new RegistrationToolError(
                                RegistrationToolErrorCodes.RegistrationCannotBeCompleted,
                                null,
                                "Registration cannot be completed until all validation issues are resolved.")
                        };

                        return (current with
                        {
                            Status = ConversationSessionStatus.Active,
                            RegistrationDraft = draft,
                            LastActivityAt = now
                        }).RecordEvent(
                            ValidationFailedEventType,
                            "Registration completion validation failed.",
                            now);
                    }

                    var fakeHfuRegistrationService = _fakeHfuRegistrationService
                        ?? throw new InvalidOperationException("Fake HFU registration service is not configured.");
                    var finalRegistration = FinalRegistrationDtoMapper.Map(draft);
                    var response = await fakeHfuRegistrationService.RegisterAsync(
                        finalRegistration,
                        mutationCancellationToken);
                    var registrationResult = new RegistrationResult(
                        response.RegistrationId,
                        response.CompletedAt);
                    completion = new RegistrationCompletionDetails(
                        finalRegistration,
                        registrationResult);

                    var completing = (current with
                    {
                        Status = ConversationSessionStatus.Completing,
                        RegistrationDraft = draft,
                        LastActivityAt = now
                    }).RecordEvent(
                        RegistrationCompletingEventType,
                        "Registration completion started.",
                        now);

                    return completing
                        .MarkCompleted(registrationResult)
                        .RecordEvent(
                            RegistrationCompletedEventType,
                            response.Message,
                            response.CompletedAt);
                },
                cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return RegistrationToolResult.Failure(
                null,
                new[]
                {
                    new RegistrationToolError(
                        RegistrationToolErrorCodes.SessionNotFound,
                        null,
                        $"Conversation session '{sessionId}' was not found.")
                });
        }

        var state = CreateState(updated);

        if (errors.Count > 0)
        {
            _logger?.LogWarning(
                "CompleteRegistrationAsync failed for session {SessionId} with {ErrorCount} error(s): {Errors}",
                sessionId,
                errors.Count,
                string.Join("; ", errors.Select(e => e.Message)));
        }
        else if (completion is not null)
        {
            if (_repository is null)
            {
                _logger?.LogWarning(
                    "CompleteRegistrationAsync succeeded for session {SessionId}, but IRegistrationRepository is null (DefaultConnection string missing in config). DB save skipped.",
                    sessionId);
            }
            else
            {
                await _repository.SaveCompletedRegistrationAsync(
                    sessionId,
                    completion.FinalRegistration,
                    completion.RegistrationResult,
                    cancellationToken);

                await _repository.SaveSessionRecordAsync(
                    updated,
                    cancellationToken);

                _logger?.LogInformation(
                    "Successfully saved completed registration for session {SessionId} (DemoId: {DemoId}) to PostgreSQL DB!",
                    sessionId,
                    completion.RegistrationResult.RegistrationId);
            }
        }

        return errors.Count == 0
            ? RegistrationToolResult.Success(state, completion)
            : RegistrationToolResult.Failure(state, errors, completion);
    }

    private async Task<RegistrationToolResult> MutateFieldNamesAsync(
        Guid sessionId,
        IReadOnlyCollection<string> fieldNames,
        Func<IRegistrationFieldDefinition, RegistrationDraft, FieldOperationResult> apply,
        string eventType,
        string eventMessage,
        CancellationToken cancellationToken)
    {
        return await MutateAsync(
            sessionId,
            current =>
            {
                var draft = current.RegistrationDraft;
                var errors = new List<RegistrationToolError>();

                foreach (var fieldName in fieldNames)
                {
                    if (!_fieldRegistry.TryGet(fieldName, out var field))
                    {
                        errors.Add(UnknownField(fieldName));
                        continue;
                    }

                    var operationResult = apply(field, draft);
                    if (operationResult.Error is not null)
                    {
                        errors.Add(operationResult.Error);
                        continue;
                    }

                    draft = operationResult.Draft;
                }

                return errors.Count > 0
                    ? ToolMutation.Invalid(current, errors)
                    : ToolMutation.Valid(draft, eventType, eventMessage);
            },
            cancellationToken);
    }

    private async Task<RegistrationToolResult> MutateAsync(
        Guid sessionId,
        Func<ConversationSession, ToolMutation> buildMutation,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<RegistrationToolError> mutationErrors = Array.Empty<RegistrationToolError>();
            var updated = await _store.UpdateAsync(
                sessionId,
                (current, _) =>
                {
                    var mutation = buildMutation(current);
                    if (!mutation.IsValid)
                    {
                        throw new RegistrationToolValidationException(
                            current,
                            mutation.Errors);
                    }

                    mutationErrors = mutation.Errors;
                    var now = _timeProvider.GetUtcNow();
                    var activeSession = current with
                    {
                        Status = ConversationSessionStatus.Active,
                        RegistrationDraft = mutation.Draft,
                        LastActivityAt = now
                    };

                    return Task.FromResult(activeSession.RecordEvent(
                        mutation.EventType,
                        mutation.EventMessage,
                        now));
                },
                cancellationToken);

            var resultState = CreateState(updated);
            return mutationErrors.Count == 0
                ? RegistrationToolResult.Success(resultState)
                : RegistrationToolResult.Failure(resultState, mutationErrors);
        }
        catch (KeyNotFoundException)
        {
            return RegistrationToolResult.Failure(
                null,
                new[]
                {
                    new RegistrationToolError(
                        RegistrationToolErrorCodes.SessionNotFound,
                        null,
                        $"Conversation session '{sessionId}' was not found.")
                });
        }
        catch (RegistrationToolValidationException exception)
        {
            return RegistrationToolResult.Failure(
                CreateState(exception.Session),
                exception.Errors);
        }
    }

    private async Task<RegistrationToolResult> FailureFromCurrentStateAsync(
        Guid sessionId,
        RegistrationToolError error,
        CancellationToken cancellationToken)
    {
        var session = await _store.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return RegistrationToolResult.Failure(
                null,
                new[]
                {
                    new RegistrationToolError(
                        RegistrationToolErrorCodes.SessionNotFound,
                        null,
                        $"Conversation session '{sessionId}' was not found.")
                });
        }

        return RegistrationToolResult.Failure(CreateState(session), new[] { error });
    }

    private RegistrationStateSnapshot CreateState(ConversationSession session)
    {
        var validation = RegistrationCompletionValidator.Evaluate(session.RegistrationDraft);
        var issues = validation.Issues;

        var missingRequiredFields = issues
            .Where(issue => issue.Code is RegistrationValidationCodes.RequiredFieldMissing
                or RegistrationValidationCodes.PersonalDataConsentRequired
                or RegistrationValidationCodes.RegistrationConfirmationRequired)
            .Select(issue => issue.Field)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var fieldsRequiringClarification = _fieldRegistry.GetSnapshots(session.RegistrationDraft)
            .Where(field => field.Status == RegistrationFieldStatus.NeedsClarification)
            .Select(field => field.Name)
            .Concat(issues
                .Where(issue => issue.Code == RegistrationValidationCodes.NeedsClarification)
                .Select(issue => issue.Field))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var fieldsAwaitingConfirmation = issues
            .Where(issue => issue.Code == RegistrationValidationCodes.RequiresConfirmation)
            .Select(issue => issue.Field)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new RegistrationStateSnapshot(
            session.SessionId,
            session.Version,
            _fieldRegistry.GetSnapshots(session.RegistrationDraft),
            missingRequiredFields,
            fieldsRequiringClarification,
            fieldsAwaitingConfirmation,
            validation.CanComplete,
            issues);
    }

    private RegistrationToolConversionContext CreateConversionContext()
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return new RegistrationToolConversionContext(today, _regionResolver);
    }

    private static RegistrationToolError UnknownField(string? fieldName)
    {
        return new RegistrationToolError(
            RegistrationToolErrorCodes.UnknownField,
            fieldName,
            $"Registration field '{fieldName}' is not supported.");
    }

    private static RegistrationCompletionDetails CreateCompletionDetails(
        ConversationSession session)
    {
        if (session.RegistrationResult is null)
        {
            throw new InvalidOperationException("Completed registration result is missing.");
        }

        return new RegistrationCompletionDetails(
            FinalRegistrationDtoMapper.Map(session.RegistrationDraft),
            session.RegistrationResult);
    }

    private sealed record ToolMutation(
        bool IsValid,
        RegistrationDraft Draft,
        string EventType,
        string EventMessage,
        IReadOnlyList<RegistrationToolError> Errors)
    {
        public static ToolMutation Valid(
            RegistrationDraft draft,
            string eventType,
            string eventMessage)
        {
            return new ToolMutation(true, draft, eventType, eventMessage, Array.Empty<RegistrationToolError>());
        }

        public static ToolMutation ValidWithErrors(
            RegistrationDraft draft,
            string eventType,
            string eventMessage,
            IReadOnlyList<RegistrationToolError> errors)
        {
            return new ToolMutation(true, draft, eventType, eventMessage, errors);
        }

        public static ToolMutation Invalid(
            ConversationSession session,
            IReadOnlyList<RegistrationToolError> errors)
        {
            return new ToolMutation(false, session.RegistrationDraft, string.Empty, string.Empty, errors);
        }
    }

    private sealed class RegistrationToolValidationException : Exception
    {
        public RegistrationToolValidationException(
            ConversationSession session,
            IReadOnlyList<RegistrationToolError> errors)
        {
            Session = session;
            Errors = errors;
        }

        public ConversationSession Session { get; }

        public IReadOnlyList<RegistrationToolError> Errors { get; }
    }

    private sealed record RegistrationToolConversionContext(
        DateOnly Today,
        IRegionResolver RegionResolver);

    private sealed class RegistrationFieldRegistry
    {
        public static RegistrationFieldRegistry Instance { get; } = new();

        private readonly IReadOnlyDictionary<string, IRegistrationFieldDefinition> _fields;

        private RegistrationFieldRegistry()
        {
            var fields = new IRegistrationFieldDefinition[]
            {
                StringField(
                    RegistrationFieldNames.FirstName,
                    draft => draft.FirstName,
                    (draft, field) => draft with { FirstName = field }),
                StringField(
                    RegistrationFieldNames.LastName,
                    draft => draft.LastName,
                    (draft, field) => draft with { LastName = field }),
                StringField(
                    RegistrationFieldNames.Patronymic,
                    draft => draft.Patronymic,
                    (draft, field) => draft with { Patronymic = field }),
                DateField(),
                PhoneField(),
                EmailField(),
                RegionField(
                    RegistrationFieldNames.CurrentRegion,
                    draft => draft.CurrentRegion,
                    (draft, field) => draft with { CurrentRegion = field }),
                StringField(
                    RegistrationFieldNames.CurrentCity,
                    draft => draft.CurrentCity,
                    (draft, field) => draft with { CurrentCity = field }),
                StringField(
                    RegistrationFieldNames.ActualAddress,
                    draft => draft.ActualAddress,
                    (draft, field) => draft with { ActualAddress = field }),
                UserCategoryField(),
                RegionField(
                    RegistrationFieldNames.RegionBeforeWar,
                    draft => draft.RegionBeforeWar,
                    (draft, field) => draft with { RegionBeforeWar = field }),
                CertificateYearField(),
                BooleanField(
                    RegistrationFieldNames.PersonalDataConsent,
                    draft => draft.PersonalDataConsent,
                    (draft, value) => draft with { PersonalDataConsent = value }),
                BooleanField(
                    RegistrationFieldNames.RegistrationConfirmed,
                    draft => draft.RegistrationConfirmed,
                    (draft, value) => draft with { RegistrationConfirmed = value })
            };

            _fields = fields.ToDictionary(field => field.Name, StringComparer.Ordinal);
        }

        public bool TryGet(string? name, out IRegistrationFieldDefinition field)
        {
            if (name is null)
            {
                field = NullRegistrationFieldDefinition.Instance;
                return false;
            }

            return _fields.TryGetValue(name, out field!);
        }

        public IReadOnlyList<RegistrationFieldSnapshot> GetSnapshots(RegistrationDraft draft)
        {
            return _fields.Values
                .Select(field => field.CreateSnapshot(draft))
                .ToArray();
        }

        private static RegistrationFieldDefinition<string> StringField(
            string name,
            Func<RegistrationDraft, RegistrationField<string>> get,
            Func<RegistrationDraft, RegistrationField<string>, RegistrationDraft> set)
        {
            return new RegistrationFieldDefinition<string>(
                name,
                get,
                set,
                static (value, _) =>
                {
                    if (!TryGetString(value, out var text))
                    {
                        return FieldConversion<string>.Invalid("Value must be a non-empty string.");
                    }

                    var normalized = NormalizeText(text);
                    return string.IsNullOrWhiteSpace(normalized)
                        ? FieldConversion<string>.Invalid("Value must be a non-empty string.")
                        : FieldConversion<string>.Valid(normalized);
                });
        }

        private static RegistrationFieldDefinition<DateOnly> DateField()
        {
            return new RegistrationFieldDefinition<DateOnly>(
                RegistrationFieldNames.DateOfBirth,
                draft => draft.DateOfBirth,
                (draft, field) => draft with { DateOfBirth = field },
                (value, context) =>
                {
                    if (value is DateOnly date)
                    {
                        return ValidateBirthDate(date, context);
                    }

                    if (!TryGetString(value, out var text)
                        || !DateOnly.TryParseExact(
                            text.Trim(),
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out date))
                    {
                        return FieldConversion<DateOnly>.Invalid(
                            "Date value must use ISO format yyyy-MM-dd.");
                    }

                    return ValidateBirthDate(date, context);
                });
        }

        private static RegistrationFieldDefinition<string> RegionField(
            string name,
            Func<RegistrationDraft, RegistrationField<string>> get,
            Func<RegistrationDraft, RegistrationField<string>, RegistrationDraft> set)
        {
            return new RegistrationFieldDefinition<string>(
                name,
                get,
                set,
                (value, context) =>
                {
                    if (!TryGetString(value, out var text))
                    {
                        return FieldConversion<string>.Invalid("Region value must be a non-empty string.");
                    }

                    var rawValue = text.Trim();
                    if (string.IsNullOrWhiteSpace(rawValue))
                    {
                        return FieldConversion<string>.Invalid("Region value must be a non-empty string.");
                    }

                    var resolution = context.RegionResolver.Resolve(rawValue);
                    return resolution.Status switch
                    {
                        RegionResolutionStatus.Resolved => FieldConversion<string>.Valid(
                            resolution.Region!.Name,
                            resolution.Region.Id),
                        RegionResolutionStatus.Ambiguous => FieldConversion<string>.ClarificationRequired(
                            rawValue,
                            "Region match is ambiguous and needs clarification.",
                            new RegistrationToolError(
                                RegistrationToolErrorCodes.RegionAmbiguous,
                                name,
                                "Region value is ambiguous.",
                                resolution.Suggestions.Select(region => region.Name).ToArray())),
                        _ => FieldConversion<string>.ClarificationRequired(
                            rawValue,
                            "Region was not found in reference data.",
                            new RegistrationToolError(
                                RegistrationToolErrorCodes.RegionNotFound,
                                name,
                                "Region value was not found in reference data."))
                    };
                });
        }

        private static RegistrationFieldDefinition<string> PhoneField()
        {
            return new RegistrationFieldDefinition<string>(
                RegistrationFieldNames.PhoneNumber,
                draft => draft.PhoneNumber,
                (draft, field) => draft with { PhoneNumber = field },
                static (value, _) =>
                {
                    if (!TryGetString(value, out var text))
                    {
                        return FieldConversion<string>.Invalid("Phone number must be a string.");
                    }

                    var trimmed = text.Trim();
                    var digits = new string(trimmed.Where(char.IsDigit).ToArray());
                    if (digits.Length is < 7 or > 15)
                    {
                        return FieldConversion<string>.Invalid(
                            "Phone number must contain between 7 and 15 digits.");
                    }

                    var normalized = trimmed.StartsWith('+')
                        ? $"+{digits}"
                        : digits;

                    return FieldConversion<string>.Valid(normalized);
                });
        }

        private static RegistrationFieldDefinition<string> EmailField()
        {
            return new RegistrationFieldDefinition<string>(
                RegistrationFieldNames.Email,
                draft => draft.Email,
                (draft, field) => draft with { Email = field },
                static (value, _) =>
                {
                    if (!TryGetString(value, out var text))
                    {
                        return FieldConversion<string>.Invalid("Email must be a string.");
                    }

                    var normalized = text.Trim();
                    try
                    {
                        var address = new MailAddress(normalized);
                        return string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase)
                            ? FieldConversion<string>.Valid(normalized)
                            : FieldConversion<string>.Invalid("Email is not valid.");
                    }
                    catch (FormatException)
                    {
                        return FieldConversion<string>.Invalid("Email is not valid.");
                    }
                });
        }

        private static RegistrationFieldDefinition<UserCategory> UserCategoryField()
        {
            return new RegistrationFieldDefinition<UserCategory>(
                RegistrationFieldNames.UserCategory,
                draft => draft.UserCategory,
                (draft, field) => draft with { UserCategory = field },
                static (value, _) =>
                {
                    if (value is UserCategory category)
                    {
                        return FieldConversion<UserCategory>.Valid(category);
                    }

                    if (!TryGetString(value, out var text))
                    {
                        return FieldConversion<UserCategory>.Invalid("User category must be a string.");
                    }

                    var key = NormalizeCategoryKey(text);
                    UserCategory? parsedCategory = key switch
                    {
                        "internallydisplacedperson" or "idp" => UserCategory.InternallyDisplacedPerson,
                        "hasmanychildren" => UserCategory.HasManyChildren,
                        "disabledperson" => UserCategory.DisabledPerson,
                        "militaryperson" => UserCategory.MilitaryPerson,
                        "militarypersonrelative" => UserCategory.MilitaryPersonRelative,
                        "other" => UserCategory.Other,
                        _ => (UserCategory?)null
                    };

                    return parsedCategory is null
                        ? FieldConversion<UserCategory>.Invalid("User category is not supported.")
                        : FieldConversion<UserCategory>.Valid(parsedCategory.Value);
                });
        }

        private static RegistrationFieldDefinition<int> CertificateYearField()
        {
            return new RegistrationFieldDefinition<int>(
                RegistrationFieldNames.DisplacedCertificateYear,
                draft => draft.DisplacedCertificateYear,
                (draft, field) => draft with { DisplacedCertificateYear = field },
                static (value, context) =>
                {
                    if (!TryGetInt(value, out var year))
                    {
                        return FieldConversion<int>.Invalid("Certificate year must be a number.");
                    }

                    return year < 2014 || year > context.Today.Year
                        ? FieldConversion<int>.Invalid(
                            $"Certificate year must be between 2014 and {context.Today.Year}.")
                        : FieldConversion<int>.Valid(year);
                });
        }

        private static BooleanFieldDefinition BooleanField(
            string name,
            Func<RegistrationDraft, bool> get,
            Func<RegistrationDraft, bool, RegistrationDraft> set)
        {
            return new BooleanFieldDefinition(name, get, set);
        }

        private static FieldConversion<DateOnly> ValidateBirthDate(
            DateOnly date,
            RegistrationToolConversionContext context)
        {
            if (date > context.Today)
            {
                return FieldConversion<DateOnly>.Invalid("Date of birth cannot be in the future.");
            }

            return date.Year < 1900
                ? FieldConversion<DateOnly>.Invalid("Date of birth must be 1900 or later.")
                : FieldConversion<DateOnly>.Valid(date);
        }

        private static bool TryGetString(object? value, out string text)
        {
            switch (value)
            {
                case string stringValue:
                    text = stringValue;
                    return true;
                case JsonElement { ValueKind: JsonValueKind.String } element:
                    text = element.GetString() ?? string.Empty;
                    return true;
                case JsonElement { ValueKind: JsonValueKind.Number } element:
                    text = element.GetRawText();
                    return true;
                default:
                    text = string.Empty;
                    return false;
            }
        }

        private static bool TryGetInt(object? value, out int number)
        {
            switch (value)
            {
                case int intValue:
                    number = intValue;
                    return true;
                case long longValue when longValue is >= int.MinValue and <= int.MaxValue:
                    number = (int)longValue;
                    return true;
                case JsonElement { ValueKind: JsonValueKind.Number } element:
                    return element.TryGetInt32(out number);
                case string stringValue:
                    return int.TryParse(
                        stringValue.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out number);
                case JsonElement { ValueKind: JsonValueKind.String } element:
                    return int.TryParse(
                        element.GetString()?.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out number);
                default:
                    number = 0;
                    return false;
            }
        }

        private static bool TryGetBool(object? value, out bool boolean)
        {
            switch (value)
            {
                case bool boolValue:
                    boolean = boolValue;
                    return true;
                case JsonElement { ValueKind: JsonValueKind.True }:
                    boolean = true;
                    return true;
                case JsonElement { ValueKind: JsonValueKind.False }:
                    boolean = false;
                    return true;
                case string stringValue:
                    return TryParseBoolText(stringValue, out boolean);
                case JsonElement { ValueKind: JsonValueKind.String } element:
                    return TryParseBoolText(element.GetString(), out boolean);
                default:
                    boolean = false;
                    return false;
            }
        }

        private static bool TryParseBoolText(string? text, out bool boolean)
        {
            switch (text?.Trim().ToLowerInvariant())
            {
                case "true":
                case "yes":
                case "1":
                    boolean = true;
                    return true;
                case "false":
                case "no":
                case "0":
                    boolean = false;
                    return true;
                default:
                    boolean = false;
                    return false;
            }
        }

        private static string NormalizeText(string value)
        {
            return string.Join(
                ' ',
                value.Trim().Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
        }

        private static string NormalizeCategoryKey(string value)
        {
            var lettersAndDigits = value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray();

            return new string(lettersAndDigits);
        }

        private sealed class RegistrationFieldDefinition<T> : IRegistrationFieldDefinition
        {
            private readonly Func<RegistrationDraft, RegistrationField<T>> _get;
            private readonly Func<RegistrationDraft, RegistrationField<T>, RegistrationDraft> _set;
            private readonly Func<object?, RegistrationToolConversionContext, FieldConversion<T>> _convert;

            public RegistrationFieldDefinition(
                string name,
                Func<RegistrationDraft, RegistrationField<T>> get,
                Func<RegistrationDraft, RegistrationField<T>, RegistrationDraft> set,
                Func<object?, RegistrationToolConversionContext, FieldConversion<T>> convert)
            {
                Name = name;
                _get = get;
                _set = set;
                _convert = convert;
            }

            public string Name { get; }

            public RegistrationFieldSnapshot CreateSnapshot(RegistrationDraft draft)
            {
                var field = _get(draft);
                return new RegistrationFieldSnapshot(
                    Name,
                    field.Value,
                    field.RawValue,
                    field.Status,
                    field.ClarificationReason,
                    field.ReferenceId);
            }

            public FieldOperationResult Update(
                RegistrationDraft draft,
                RegistrationFieldUpdate update,
                RegistrationToolConversionContext context)
            {
                var conversion = _convert(update.Value, context);
                if (conversion.NeedsClarification)
                {
                    return FieldOperationResult.Handled(_set(
                        draft,
                        RegistrationField<T>.NeedsClarification(
                            default,
                            update.RawValue ?? conversion.RawValue ?? ToRawValue(update.Value),
                            conversion.ClarificationReason)),
                        conversion.Error!);
                }

                if (!conversion.Succeeded)
                {
                    return FieldOperationResult.Invalid(InvalidValue(Name, conversion.ErrorMessage));
                }

                return FieldOperationResult.Valid(_set(
                    draft,
                    RegistrationField<T>.Captured(
                        conversion.Value,
                        update.RawValue ?? ToRawValue(update.Value),
                        conversion.ReferenceId)));
            }

            public FieldOperationResult Confirm(RegistrationDraft draft)
            {
                var field = _get(draft);
                if (!HasFieldValue(field))
                {
                    return FieldOperationResult.Invalid(new RegistrationToolError(
                        RegistrationToolErrorCodes.FieldCannotBeConfirmed,
                        Name,
                        "Field cannot be confirmed before it has a value."));
                }

                return FieldOperationResult.Valid(_set(
                    draft,
                    RegistrationField<T>.Confirmed(field.Value!, field.RawValue, field.ReferenceId)));
            }

            public FieldOperationResult MarkNeedsClarification(
                RegistrationDraft draft,
                string? reason)
            {
                var field = _get(draft);
                return FieldOperationResult.Valid(_set(
                    draft,
                    RegistrationField<T>.NeedsClarification(
                        field.Value,
                        field.RawValue,
                        string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                        field.ReferenceId)));
            }

            public FieldOperationResult Clear(RegistrationDraft draft)
            {
                return FieldOperationResult.Valid(_set(draft, RegistrationField<T>.Missing()));
            }

            private static bool HasFieldValue(RegistrationField<T> field)
            {
                return field.Value switch
                {
                    null => false,
                    string text => !string.IsNullOrWhiteSpace(text),
                    _ => true
                };
            }
        }

        private sealed class BooleanFieldDefinition : IRegistrationFieldDefinition
        {
            private readonly Func<RegistrationDraft, bool> _get;
            private readonly Func<RegistrationDraft, bool, RegistrationDraft> _set;

            public BooleanFieldDefinition(
                string name,
                Func<RegistrationDraft, bool> get,
                Func<RegistrationDraft, bool, RegistrationDraft> set)
            {
                Name = name;
                _get = get;
                _set = set;
            }

            public string Name { get; }

            public RegistrationFieldSnapshot CreateSnapshot(RegistrationDraft draft)
            {
                var value = _get(draft);
                return new RegistrationFieldSnapshot(
                    Name,
                    value,
                    null,
                    value ? RegistrationFieldStatus.Confirmed : RegistrationFieldStatus.Missing,
                    null,
                    null);
            }

            public FieldOperationResult Update(
                RegistrationDraft draft,
                RegistrationFieldUpdate update,
                RegistrationToolConversionContext context)
            {
                return TryGetBool(update.Value, out var value)
                    ? FieldOperationResult.Valid(_set(draft, value))
                    : FieldOperationResult.Invalid(InvalidValue(Name, "Value must be boolean."));
            }

            public FieldOperationResult Confirm(RegistrationDraft draft)
            {
                return _get(draft)
                    ? FieldOperationResult.Valid(draft)
                    : FieldOperationResult.Invalid(new RegistrationToolError(
                        RegistrationToolErrorCodes.FieldCannotBeConfirmed,
                        Name,
                        "Boolean field cannot be confirmed before it is true."));
            }

            public FieldOperationResult MarkNeedsClarification(
                RegistrationDraft draft,
                string? reason)
            {
                return FieldOperationResult.Invalid(new RegistrationToolError(
                    RegistrationToolErrorCodes.UnsupportedFieldOperation,
                    Name,
                    "Boolean field does not support clarification status."));
            }

            public FieldOperationResult Clear(RegistrationDraft draft)
            {
                return FieldOperationResult.Valid(_set(draft, false));
            }
        }

        private sealed class NullRegistrationFieldDefinition : IRegistrationFieldDefinition
        {
            public static NullRegistrationFieldDefinition Instance { get; } = new();

            public string Name => string.Empty;

            public RegistrationFieldSnapshot CreateSnapshot(RegistrationDraft draft)
            {
                throw new NotSupportedException();
            }

            public FieldOperationResult Update(
                RegistrationDraft draft,
                RegistrationFieldUpdate update,
                RegistrationToolConversionContext context)
            {
                throw new NotSupportedException();
            }

            public FieldOperationResult Confirm(RegistrationDraft draft)
            {
                throw new NotSupportedException();
            }

            public FieldOperationResult MarkNeedsClarification(
                RegistrationDraft draft,
                string? reason)
            {
                throw new NotSupportedException();
            }

            public FieldOperationResult Clear(RegistrationDraft draft)
            {
                throw new NotSupportedException();
            }
        }

        private static string? ToRawValue(object? value)
        {
            return value switch
            {
                null => null,
                JsonElement element => element.ValueKind == JsonValueKind.String
                    ? element.GetString()
                    : element.GetRawText(),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)
            };
        }

        private static RegistrationToolError InvalidValue(string fieldName, string message)
        {
            return new RegistrationToolError(
                RegistrationToolErrorCodes.InvalidFieldValue,
                fieldName,
                message);
        }
    }

    private interface IRegistrationFieldDefinition
    {
        string Name { get; }

        RegistrationFieldSnapshot CreateSnapshot(RegistrationDraft draft);

        FieldOperationResult Update(
            RegistrationDraft draft,
            RegistrationFieldUpdate update,
            RegistrationToolConversionContext context);

        FieldOperationResult Confirm(RegistrationDraft draft);

        FieldOperationResult MarkNeedsClarification(
            RegistrationDraft draft,
            string? reason);

        FieldOperationResult Clear(RegistrationDraft draft);
    }

    private sealed record FieldOperationResult(
        RegistrationDraft Draft,
        RegistrationToolError? Error,
        bool ShouldPersist)
    {
        public static FieldOperationResult Valid(RegistrationDraft draft)
        {
            return new FieldOperationResult(draft, null, true);
        }

        public static FieldOperationResult Handled(
            RegistrationDraft draft,
            RegistrationToolError error)
        {
            return new FieldOperationResult(draft, error, true);
        }

        public static FieldOperationResult Invalid(RegistrationToolError error)
        {
            return new FieldOperationResult(RegistrationDraft.Create(), error, false);
        }
    }

    private sealed record FieldConversion<T>(
        bool Succeeded,
        bool NeedsClarification,
        T Value,
        string ErrorMessage,
        string? ReferenceId,
        string? RawValue,
        string? ClarificationReason,
        RegistrationToolError? Error)
    {
        public static FieldConversion<T> Valid(
            T value,
            string? referenceId = null)
        {
            return new FieldConversion<T>(
                true,
                false,
                value,
                string.Empty,
                referenceId,
                null,
                null,
                null);
        }

        public static FieldConversion<T> Invalid(string errorMessage)
        {
            return new FieldConversion<T>(
                false,
                false,
                default!,
                errorMessage,
                null,
                null,
                null,
                null);
        }

        public static FieldConversion<T> ClarificationRequired(
            string rawValue,
            string clarificationReason,
            RegistrationToolError error)
        {
            return new FieldConversion<T>(
                false,
                true,
                default!,
                string.Empty,
                null,
                rawValue,
                clarificationReason,
                error);
        }
    }
}
