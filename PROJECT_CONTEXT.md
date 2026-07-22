# HFU Voice AI Registration Assistant PoC - Project Context

## Imported Context

This project continues the Codex task `HFU_Voice_AI_Registration` / `Изучить требования AI-сервиса HFU`.

Source materials now available in this workspace:

- `HFU Voice AI Registration Assistant Technical Specification.docx`
- `HFU Voice AI Registration Assistant PoC.docx`
- `context/HFU Voice AI Registration Assistant Technical Specification.txt`
- `context/HFU Voice AI Registration Assistant PoC.txt`

The prior task established that the new working project should live at:

`C:\git\Hfu.VoiceRegistration`

The existing HFU backend lives at:

`C:\git\HFU\backend`

The HFU backend is reference-only for this PoC. Do not modify it unless explicitly requested.

## Product Goal

Build an external advertising proof of concept for HFU: a browser-based voice AI assistant that demonstrates primary user registration through a natural spoken conversation.

The PoC is intended to help HFU stakeholders evaluate and approve a future contract. It is not a production HFU integration and must not store real personal data in a permanent database.

## Core Architecture

Primary principle:

- Backend owns conversation and registration state.
- OpenAI owns speech-to-speech processing.
- Browser owns WebRTC audio transport and UI display.

Future flow:

- Browser connects to OpenAI Realtime API through WebRTC.
- Backend issues safe short-lived Realtime credentials/configuration.
- OpenAI Realtime model conducts the spoken conversation.
- Tool calls are routed through the browser data channel to backend handlers.
- Backend validates and updates registration state.
- SignalR pushes live state and diagnostic updates to the frontend.
- Fake HFU registration service returns a demo registration ID after successful validation and confirmation.

The architecture should allow a future SIP/IP telephony transport to replace browser WebRTC without rewriting registration logic.

## Stage 1 Scope

Current agreed scope is only `Этап 1. Каркас solution`.

Create the solution and base projects, but do not implement registration business logic or voice integration yet.

Expected Stage 1 output:

- Solution `Hfu.VoiceRegistration`.
- ASP.NET Core Web API backend.
- Layered backend projects for Domain, Application, Infrastructure, and Api.
- xUnit test projects.
- React + TypeScript + Vite frontend.
- `GET /health` endpoint.
- Frontend page that calls `/health` and displays backend health.
- Configuration placeholders for future OpenAI and frontend settings, without real secrets.
- README with build/test/run instructions.
- Architecture document describing high-level separation and future direction.
- `.gitignore`.

Prior recommendation: target `.NET 8` for compatibility with the existing HFU backend, even though the machine has a newer SDK installed.

## Explicit Stage 1 Non-Goals

Do not add or implement these in Stage 1:

- OpenAI SDK or Realtime API integration.
- WebRTC.
- SignalR hub.
- Backend AI tools.
- Registration domain/business rules.
- Fake HFU registration.
- EF Core, database packages, Redis packages, or persistent storage.
- Production HFU integration.
- Real API keys or secrets.

## Required Backend Shape

Projects expected by the PoC document:

- `Hfu.VoiceRegistration.Domain`
- `Hfu.VoiceRegistration.Application`
- `Hfu.VoiceRegistration.Infrastructure`
- `Hfu.VoiceRegistration.Api`
- `Hfu.VoiceRegistration.Domain.Tests`
- `Hfu.VoiceRegistration.Application.Tests`
- `Hfu.VoiceRegistration.Api.IntegrationTests`

`Program.cs` should stay small. Use dependency injection extension methods for layer registrations.

The API should expose:

- `GET /health`

The health response should be simple and frontend-friendly, for example status/service/time/version fields.

Avoid a direct `Api -> Domain` reference unless required for the minimal Stage 1 compile. Prefer access through Application contracts.

## Required Frontend Shape

Frontend project:

- `Hfu.VoiceRegistration.Web`
- React + TypeScript + Vite
- No OpenAI SDK
- No WebRTC
- No SignalR in Stage 1

The first page should show:

- `HFU Voice Registration Demo`
- Backend health state from `GET /health`
- Loading, healthy, and error states

Use a small typed API client for the health endpoint.

## Future Stages From Specification

After Stage 1, the technical specification outlines these later stages:

- Stage 2: domain model and validation rules.
- Stage 3: in-memory session management.
- Stage 4: backend registration tools.
- Stage 5: reference data.
- Stage 6: fake HFU registration.
- Stage 7: backend HTTP API.
- Stage 8: React UI without voice.
- Stage 9: SignalR.
- Stage 10: OpenAI Realtime WebRTC.
- Stage 11: tool-call bridge.
- Stage 12: registration system prompt.
- Stage 13: reconnect and recovery.
- Stage 14: developer panel.
- Stage 15: final testing and demo polish.

Stage 2 was separately requested and uses the conservative completion rule approved on 2026-07-22.

## Stage 2 Scope

Stage 2 implements pure domain model and validation rules:

- `RegistrationField<T>` and `RegistrationFieldStatus`.
- `UserCategory`.
- `RegistrationDraft`.
- `ConversationSession` domain concept.
- `RegistrationValidationResult` and validation issues.
- Completion eligibility rules covered by unit tests.

Approved conservative completion rule:

- universally required fields must be filled and not `Missing`, `NeedsClarification`, or `Rejected`;
- conditionally required fields must be filled when applicable;
- `phoneNumber`, `dateOfBirth`, `currentRegion`, `currentCity`, and `userCategory` must be `Confirmed`;
- `email`, when provided, must be `Confirmed`;
- optional fields do not block completion when `Missing` or `Rejected`;
- `InternallyDisplacedPerson` requires `regionBeforeWar` and `displacedCertificateYear`;
- `personalDataConsent` and `registrationConfirmed` must both be `true`.

Stage 2 must not add HTTP APIs, stores, OpenAI, WebRTC, SignalR, fake HFU registration, databases, Redis, or production HFU integration.

Stage 3 was separately requested and adds in-memory session management with a dedicated `Hfu.VoiceRegistration.Infrastructure.Tests` project.

## Stage 3 Scope

Stage 3 implements:

- application-level `IConversationSessionStore`;
- `ConversationSessionStoreOptions`;
- infrastructure-level `InMemoryConversationSessionStore`;
- per-session locking for mutations;
- versioning for successful mutation updates;
- event journal persistence as part of `ConversationSession`;
- expiration for unfinished and completed sessions;
- hosted cleanup service;
- infrastructure tests for store behavior and DI registration.

Default timeout values:

- unfinished inactive session: 30 minutes;
- completed session: 60 minutes;
- cleanup interval: 5 minutes.

Stage 3 still must not add HTTP APIs, OpenAI, WebRTC, SignalR, fake HFU registration, EF Core, databases, Redis, or production HFU integration.

Stage 4 was separately requested and implements application-level backend registration tools with the conservative scope approved on 2026-07-22.

## Stage 4 Scope

Stage 4 implements:

- `IRegistrationToolService`;
- `update_registration_fields`;
- `confirm_registration_fields`;
- `mark_fields_for_clarification`;
- `clear_registration_fields`;
- `get_registration_state`;
- field registry for all known registration field names;
- basic normalization and strict typed validation;
- structured tool result DTOs with current state and errors;
- Application tests for direct tool-handler usage without HTTP or OpenAI.

Confirmed Stage 4 boundary:

- do not add OpenAI SDK, Realtime API, WebRTC, SignalR, HTTP registration endpoints, fake HFU registration, EF Core, databases, Redis, or production HFU integration;
- defer actual `complete_registration` submission until the fake HFU/API stages;
- keep `registrationCanBeCompleted` in state so the future completion handler can reuse the existing validation.

Stage 5 was separately requested and implements reference data for regions.

## Stage 5 Scope

Stage 5 implements:

- Ukrainian region reference data;
- Ukrainian canonical names in stored draft values;
- Russian and Ukrainian aliases;
- `IRegionReferenceDataProvider`;
- `IRegionResolver`;
- exact and conservative fuzzy matching;
- `Resolved`, `Ambiguous`, and `NotFound` results;
- integration with `update_registration_fields` for `currentRegion` and `regionBeforeWar`;
- structured `RegionAmbiguous` and `RegionNotFound` tool errors;
- suggestions for ambiguous matches;
- server-owned `ReferenceId` on registration fields;
- Application tests for resolver behavior and tool integration.

Confirmed Stage 5 boundary:

- do not add HTTP reference data endpoints until Stage 7;
- do not add OpenAI SDK, Realtime API, WebRTC, SignalR, fake HFU registration, EF Core, databases, Redis, or production HFU integration;
- do not accept model-generated region IDs as aliases;
- ambiguous and unknown regions must be persisted as `NeedsClarification`, not silently ignored.

## Full Technical Specification Highlights

The full spec describes:

- Required registration fields.
- Optional and conditionally required fields.
- User categories.
- Actual address handling.
- Unified field state model.
- Registration draft model.
- Region normalization.
- Field confirmation rules.
- Backend tools:
  - `update_registration_fields`
  - `confirm_registration_fields`
  - `mark_fields_for_clarification`
  - `clear_registration_fields`
  - `get_registration_state`
  - `complete_registration`
- Completion rules.
- Fake HFU registration behavior.
- Conversation session state.
- In-memory storage.
- Concurrent access/versioning.
- Timeouts.
- Page refresh and recovery.
- WebRTC disconnect handling.
- HTTP API.
- SignalR events.
- Realtime tool call flow.
- Voice assistant behavioral rules.
- Transcript/event journal.
- UI panels.
- Error handling.
- Security rules.
- Unit, integration, and manual voice tests.
- Definition of Done.

Refer to the extracted text files in `context/` for the complete document text.

## Security And Data Rules

- Permanent OpenAI API key must exist only on backend.
- Frontend receives only short-lived data required for Realtime connection.
- Backend must not trust field names, values, types, or statuses from the model without validation.
- README must say the PoC is not intended to process real personal data.
- Do not log the full final registration DTO in production-style logs.

## Local Environment Notes From Prior Task

- Existing HFU backend: `C:\git\HFU\backend`, read-only reference.
- HFU backend appears to use `.NET 8`.
- Installed SDK noted previously: `.NET SDK 10.0.301`.
- Node noted previously: `v24.11.1`.
- `npm.cmd` works; plain `npm` in PowerShell may be blocked by execution policy.
- Use `npm.cmd` in PowerShell commands if npm is needed.
