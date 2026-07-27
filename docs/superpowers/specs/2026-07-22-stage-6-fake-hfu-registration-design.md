# Stage 6 Fake HFU Registration Design

## Status

Approved by the user on 2026-07-22.

User decisions:

- repeated completion must not generate a new ID;
- repeated completion returns `RegistrationAlreadyCompleted` with existing result and state;
- fake HFU service does not need an artificial failure scenario in Stage 6;
- demo registration IDs use `DEMO-{year}-{counter:000000}`.

## Goal

Implement backend completion workflow and fake HFU registration without adding HTTP endpoints, OpenAI, Realtime/WebRTC, SignalR, database persistence, Redis, EF Core, or production HFU integration.

## Architecture

`Hfu.VoiceRegistration.Application` owns the completion workflow, final DTO mapper, fake HFU service contract, and `complete_registration` tool entry point.

`Hfu.VoiceRegistration.Infrastructure` owns the in-memory fake HFU adapter and demo registration ID generator.

The model/tool caller sends only `personalDataConsent` and `registrationConfirmed`. The backend updates those flags, reruns `RegistrationCompletionValidator`, maps the final DTO from `RegistrationDraft`, and sends only backend-generated data to the fake HFU service.

## Completion Flow

1. Load the conversation session.
2. If the session is already completed, return `RegistrationAlreadyCompleted` with existing state and result.
3. Update consent and final confirmation flags from the request.
4. Validate the full draft.
5. If validation fails, persist the updated flags and return structured validation failure.
6. Map `FinalRegistrationDto` from the server-owned draft.
7. Submit the final DTO to fake HFU.
8. Store `RegistrationResult` on the session and mark the session `Completed`.
9. Record completion events in the session journal.

## Final DTO

The final DTO includes required fields, optional fields when captured, IDP-specific fields when category is `InternallyDisplacedPerson`, Ukrainian canonical region names, and server-owned region reference IDs when present.

## Boundaries

Stage 6 does not add HTTP endpoints, OpenAI, Realtime/WebRTC, SignalR, databases, Redis, EF Core, or production HFU integration. Stage 7 will expose the workflow over HTTP.

## Testing

Application tests cover:

- successful completion;
- final DTO mapping;
- failure with missing required fields;
- failure with clarification fields;
- failure without consent or final confirmation;
- repeated completion returns `RegistrationAlreadyCompleted` and does not call fake HFU again.

Infrastructure tests cover:

- demo registration ID format and sequence;
- fake HFU service returns successful result.
