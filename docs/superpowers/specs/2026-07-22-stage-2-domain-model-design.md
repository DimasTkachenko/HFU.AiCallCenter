# Stage 2 Domain Model And Validation Rules Design

## Status

Approved by the user on 2026-07-22 after confirming the conservative completion rule.

## Goal

Implement the pure domain model and registration completion validation for `Hfu.VoiceRegistration` without HTTP, OpenAI, SignalR, WebRTC, storage, or fake HFU integration.

## Domain Model

Stage 2 adds domain-only types in `Hfu.VoiceRegistration.Domain`:

- `RegistrationField<T>` with `Value`, `RawValue`, and `RegistrationFieldStatus`.
- `RegistrationFieldStatus`: `Missing`, `Captured`, `NeedsClarification`, `Confirmed`, `Rejected`.
- `UserCategory`: `InternallyDisplacedPerson`, `HasManyChildren`, `DisabledPerson`, `MilitaryPerson`, `MilitaryPersonRelative`, `Other`.
- `RegistrationDraft` with all fields from the specification.
- `ConversationSessionStatus`.
- `ConversationSession`, `RegistrationResult`, and `ConversationEvent` as domain concepts only.
- `RegistrationValidationResult` and `RegistrationValidationIssue`.
- `RegistrationCompletionValidator`.

## Completion Rule

Registration can complete only when:

- every universally required field is filled and is not `Missing`, `NeedsClarification`, or `Rejected`;
- every applicable conditionally required field is filled and is not `Missing`, `NeedsClarification`, or `Rejected`;
- `phoneNumber`, `dateOfBirth`, `currentRegion`, `currentCity`, and `userCategory` are `Confirmed`;
- `email`, when provided, is `Confirmed`;
- optional fields do not block completion when `Missing` or `Rejected`;
- `personalDataConsent` is `true`;
- `registrationConfirmed` is `true`.

For `InternallyDisplacedPerson`, `regionBeforeWar` and `displacedCertificateYear` are required. For all other categories, those fields are not applicable and do not block completion.

## Boundaries

Stage 2 does not add:

- HTTP endpoints;
- application use cases;
- session stores;
- concurrency handling;
- region resolver/reference data;
- backend tools;
- fake HFU registration;
- SignalR;
- OpenAI SDK or Realtime API;
- WebRTC.

## Testing

Domain unit tests must prove:

- fields default to `Missing`;
- draft fields initialize safely;
- user category values exist;
- required missing/rejected/clarification fields block completion;
- optional missing/rejected fields do not block completion;
- email blocks completion when captured but unconfirmed;
- conservative confirmation fields block completion unless confirmed;
- internally displaced person conditional fields are required;
- non-IDP conditional fields do not block completion;
- consent and final confirmation are required;
- conversation session initializes with a draft, status, timestamps, version, and events.
