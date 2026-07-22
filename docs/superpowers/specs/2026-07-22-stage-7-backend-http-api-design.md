# Stage 7 Backend HTTP API Design

## Status

Approved by the user on 2026-07-22.

User decisions:

- use typed REST endpoints for registration tools instead of a generic JSON dispatcher;
- expose Swagger/OpenAPI for visual/manual testing;
- return business tool failures as structured `RegistrationToolResult` responses with current state, not as bare HTTP errors.

## Goal

Expose the completed backend registration flow over HTTP so it can be exercised through Swagger or Postman without OpenAI, Realtime/WebRTC, SignalR, React UI, database persistence, Redis, EF Core, or production HFU integration.

## Architecture

`Hfu.VoiceRegistration.Api` owns HTTP-only request and response contracts plus minimal endpoint mapping. `Hfu.VoiceRegistration.Application` remains the authority for registration state changes, reference data, validation, and completion. `Hfu.VoiceRegistration.Infrastructure` continues to provide in-memory session storage and fake HFU registration.

The API layer must not duplicate registration business rules. It translates HTTP requests into application service calls, translates missing or malformed HTTP resources into Problem Details, and returns application tool results unchanged enough for future UI and OpenAI tool-call bridge consumers.

## Endpoints

Conversation sessions:

- `POST /api/conversation-sessions`
  - creates a new `ConversationSession`;
  - returns `201 Created` with `sessionId`, status, timestamps, version, and current registration state;
  - sets `Location` to `/api/conversation-sessions/{sessionId}`.
- `GET /api/conversation-sessions/{sessionId}`
  - returns session metadata, current registration state, registration result if present, and recent event journal data;
  - returns `404` Problem Details when the session does not exist.
- `POST /api/conversation-sessions/{sessionId}/abandon`
  - marks a non-completed session as `Abandoned`;
  - returns updated session metadata and state;
  - returns `409` Problem Details if the session is already completed.

Registration tools:

- `POST /api/conversation-sessions/{sessionId}/tools/update-registration-fields`
- `POST /api/conversation-sessions/{sessionId}/tools/confirm-registration-fields`
- `POST /api/conversation-sessions/{sessionId}/tools/mark-fields-for-clarification`
- `POST /api/conversation-sessions/{sessionId}/tools/clear-registration-fields`
- `POST /api/conversation-sessions/{sessionId}/tools/get-registration-state`
- `POST /api/conversation-sessions/{sessionId}/tools/complete-registration`

Each tool endpoint calls the matching `IRegistrationToolService` method and returns `200 OK` with `RegistrationToolResult`, even when the result contains business errors such as `RegistrationCannotBeCompleted`, `RegionNotFound`, or `RegistrationAlreadyCompleted`. A missing session returns `404` Problem Details.

Reference data:

- `GET /api/reference-data/regions`
  - returns Ukrainian canonical region names and server-owned IDs;
  - aliases are included for manual testing and future UI autocomplete.

OpenAPI:

- expose Swagger JSON and Swagger UI in development and test runs;
- document endpoint names, request types, and response contracts through typed minimal APIs.

## HTTP Contracts

API response contracts are stable DTOs under `Hfu.VoiceRegistration.Api.Contracts`. They can include existing enum values serialized as strings. Tool request DTOs stay narrow and match Stage 4-6 application service input:

- update request: `fields: [{ name, value, rawValue }]`;
- confirm request: `fieldNames: []`;
- mark clarification request: `fieldNames: [], reason`;
- clear request: `fieldNames: []`;
- complete request: `personalDataConsent`, `registrationConfirmed`.

The final registration DTO is never accepted from HTTP clients.

## Error Handling

Use Problem Details for HTTP-layer problems:

- `400 Bad Request` for invalid route values or malformed JSON handled by ASP.NET Core;
- `404 Not Found` for missing sessions;
- `409 Conflict` for abandoning a completed session.

Use `RegistrationToolResult.Errors` for business/tool problems:

- unsupported field names;
- invalid field values;
- region ambiguity or not found;
- completion validation failures;
- repeated completion.

## Boundaries

Stage 7 does not add OpenAI SDK, Realtime credentials, WebRTC, SignalR, React UI registration screens, EF Core, databases, Redis, persistent storage, or production HFU integration.

## Testing

Integration tests cover:

- Swagger JSON is available;
- session creation and retrieval;
- missing session returns Problem Details;
- reference regions endpoint returns Ukrainian region names;
- typed tool endpoints can update, confirm, read, and complete a registration;
- business tool errors return `200 OK` with structured errors and state;
- abandoning a session updates status and completed sessions cannot be abandoned.
