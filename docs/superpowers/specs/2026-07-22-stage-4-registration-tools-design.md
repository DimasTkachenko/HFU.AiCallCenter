# Stage 4 Backend Registration Tools Design

## Status

Approved by the user on 2026-07-22 with a conservative scope: implement application-level registration tools now, defer actual `complete_registration` submission until fake HFU/API stages.

## Goal

Implement backend registration tool handlers that mutate the server-owned `RegistrationDraft` through the application layer without adding HTTP, OpenAI, WebRTC, SignalR, fake HFU registration, database persistence, or production HFU integration.

## Architecture

`Hfu.VoiceRegistration.Application` owns `IRegistrationToolService` because future HTTP endpoints and OpenAI tool-call bridges should call the same application service. The service depends on `IConversationSessionStore` and `TimeProvider`, not on Infrastructure implementation details.

## Tools

- `update_registration_fields`: validate supported field names, parse and normalize values, capture draft fields.
- `confirm_registration_fields`: move fields with values to `Confirmed`.
- `mark_fields_for_clarification`: move registration fields to `NeedsClarification` and preserve a reason.
- `clear_registration_fields`: reset fields to `Missing` or booleans to `false`.
- `get_registration_state`: return authoritative state, missing required fields, fields requiring clarification, fields awaiting confirmation, and `RegistrationCanBeCompleted`.

## Validation

The backend does not trust model-provided field names, statuses, types, or values. Stage 4 accepts only known field names from `RegistrationFieldNames`, ignores model status, and rejects invalid values without changing the stored draft.

Basic normalization covers text trimming, phone digit normalization, ISO date parsing, email validation, user category aliases, certificate year range, and boolean parsing.

## Boundaries

Stage 4 does not add `complete_registration`, HTTP endpoints, OpenAI clients, Realtime/WebRTC, SignalR, fake HFU registration, EF Core, Redis, databases, or production HFU integration.

The final registration submission remains a later-stage concern. Stage 4 only reports whether the current draft can be completed according to the existing domain validator.

## Testing

`Hfu.VoiceRegistration.Application.Tests` covers direct service usage with a fake session store:

- successful typed field updates;
- unknown and invalid field rejection without mutation;
- confirmation behavior;
- missing-field confirmation rejection;
- clarification state and reason preservation;
- clearing fields;
- state snapshot reporting;
- unknown session errors;
- DI registration.
