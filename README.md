# Hfu.VoiceRegistration

External advertising PoC for HFU voice-assisted registration. The current implementation contains the Stage 1 runnable skeleton, Stage 2 pure domain model, Stage 3 in-memory conversation session management, Stage 4 application-level backend registration tools, Stage 5 reference data for region matching, Stage 6 fake HFU registration completion, Stage 7 backend HTTP API, Stage 8 React UI without voice, Stage 9 SignalR live updates, Stage 10 OpenAI Realtime WebRTC voice transport, and Stage 11 OpenAI Realtime tool-call bridge.

This PoC is not intended to process real personal data. Do not enter real user registration details into local demos.

## Prerequisites

- .NET 8 SDK
- Node.js 24 or compatible current Node.js runtime
- npm. In PowerShell, use `npm.cmd`.

## Project Structure

```text
src/
  Hfu.VoiceRegistration.Domain/
  Hfu.VoiceRegistration.Application/
  Hfu.VoiceRegistration.Infrastructure/
  Hfu.VoiceRegistration.Api/
  Hfu.VoiceRegistration.Web/
tests/
  Hfu.VoiceRegistration.Domain.Tests/
  Hfu.VoiceRegistration.Application.Tests/
  Hfu.VoiceRegistration.Infrastructure.Tests/
  Hfu.VoiceRegistration.Api.IntegrationTests/
docs/
  architecture.md
```

## Backend

Build and test:

```powershell
dotnet build Hfu.VoiceRegistration.sln
dotnet test Hfu.VoiceRegistration.sln
```

Run the API:

```powershell
dotnet run --project src\Hfu.VoiceRegistration.Api\Hfu.VoiceRegistration.Api.csproj --launch-profile http
```

Health endpoint:

```text
http://localhost:5076/health
```

Swagger UI:

```text
http://localhost:5076/swagger
```

Example response:

```json
{
  "status": "healthy",
  "service": "Hfu.VoiceRegistration.Api",
  "timestampUtc": "2026-07-22T12:00:00Z",
  "version": "1.0.0.0"
}
```

## Domain Model

Stage 2 adds pure domain types and rules in `src\Hfu.VoiceRegistration.Domain`:

- registration field status model;
- supported user categories;
- registration draft fields;
- conversation session concept;
- structured validation issues and result;
- conservative completion eligibility rules.

The domain model is covered by unit tests and does not depend on HTTP, OpenAI, SignalR, WebRTC, storage, or fake HFU registration.

## Session Management

Stage 3 adds in-memory conversation session storage behind the application-level `IConversationSessionStore` interface:

- multiple independent sessions are stored in memory;
- per-session locking protects concurrent mutations;
- successful mutations advance session `Version`;
- the event journal remains part of each stored session;
- inactive unfinished sessions expire after 30 minutes;
- completed sessions expire after 60 minutes;
- a hosted cleanup service runs every 5 minutes.

This is still a PoC in-memory implementation. No EF Core, Redis, database, or production persistence is used.

## Backend Registration Tools

Stage 4 adds application-level backend registration tools behind `IRegistrationToolService`:

- `update_registration_fields`
- `confirm_registration_fields`
- `mark_fields_for_clarification`
- `clear_registration_fields`
- `get_registration_state`

The tools update the server-owned `RegistrationDraft` through `IConversationSessionStore`, validate supported field names, normalize basic values, reject invalid input without changing the draft, and return a `RegistrationToolResult` with the current registration state plus structured errors.

The OpenAI tool-call bridge calls this application service through the Stage 7 HTTP API instead of editing registration state directly. Stage 8 also exercises these tools through a manual React UI.

## Reference Data

Stage 5 adds application-level Ukrainian region reference data:

- canonical region names are stored in Ukrainian;
- Russian and Ukrainian aliases are accepted for matching;
- internal region IDs are kept server-side and are not accepted as model-provided aliases;
- ambiguous or unknown region values mark the affected field as `NeedsClarification`;
- `RegistrationToolResult` returns `RegionAmbiguous` or `RegionNotFound` with Ukrainian suggestions when available.

The resolver is integrated into `update_registration_fields` for `currentRegion` and `regionBeforeWar`. Stage 7 exposes the catalog at `GET /api/reference-data/regions`.

## Fake HFU Registration

Stage 6 adds the application-level `complete_registration` workflow and an infrastructure fake HFU registration adapter:

- callers can submit only final `personalDataConsent` and `registrationConfirmed` flags;
- the backend builds the final registration DTO from server-owned draft state;
- invalid or incomplete drafts return `RegistrationCannotBeCompleted`;
- already completed sessions return `RegistrationAlreadyCompleted` with the existing completion result and state;
- fake demo IDs use `DEMO-{year}-{counter:000000}`;
- no real HFU backend is called.

Stage 7 exposes this workflow through typed HTTP endpoints. The OpenAI tool-call bridge calls those endpoints instead of submitting final registration payloads directly.

## Backend HTTP API

Stage 7 exposes the registration flow over HTTP:

- `POST /api/conversation-sessions`
- `GET /api/conversation-sessions/{sessionId}`
- `POST /api/conversation-sessions/{sessionId}/abandon`
- `POST /api/conversation-sessions/{sessionId}/tools/update-registration-fields`
- `POST /api/conversation-sessions/{sessionId}/tools/confirm-registration-fields`
- `POST /api/conversation-sessions/{sessionId}/tools/mark-fields-for-clarification`
- `POST /api/conversation-sessions/{sessionId}/tools/clear-registration-fields`
- `POST /api/conversation-sessions/{sessionId}/tools/get-registration-state`
- `POST /api/conversation-sessions/{sessionId}/tools/complete-registration`
- `GET /api/reference-data/regions`

Business tool errors return `200 OK` with structured `RegistrationToolResult` payloads and current state. Missing sessions and HTTP-layer conflicts return Problem Details.

## Frontend

Install dependencies:

```powershell
cd src\Hfu.VoiceRegistration.Web
npm.cmd install
```

Run the frontend dev server:

```powershell
npm.cmd run dev
```

Frontend URL:

```text
http://127.0.0.1:5173
```

The Vite dev server proxies `/health`, `/api`, and `/hubs` to `http://localhost:5076`.

Stage 8 frontend capabilities:

- creates and restores conversation sessions with `localStorage`;
- displays backend registration state, field statuses, completion issues, and structured tool errors;
- provides a manual demo/developer tool emulator for update, confirm, clarification, clear, state refresh, and completion;
- shows the fake HFU registration result when completion succeeds.

Stage 9 realtime capabilities:

- connects the browser to the backend SignalR hub at `/hubs/conversation`;
- joins the current conversation session group after create/restore and rejoins after reconnect;
- displays compact live connection status and recent backend events;
- refreshes full session state through HTTP after live events, keeping HTTP/backend state authoritative.

Stage 10 voice capabilities:

- adds `POST /api/conversation-sessions/{sessionId}/realtime/calls` for browser SDP offers;
- keeps the permanent OpenAI API key on the backend and calls `POST /v1/realtime/calls` with `HttpClient`;
- creates a browser WebRTC connection with microphone capture, remote audio playback, and an `oai-events` data channel;
- displays voice connection state, compact OpenAI Realtime diagnostics, and transcript entries from known Realtime events.

Stage 11 OpenAI tool-call bridge capabilities:

- adds Realtime function tool definitions to the server-owned OpenAI session payload;
- exposes update, confirm, clarification, clear, state, and completion tools to the model;
- listens for function-call events on the browser `oai-events` data channel;
- dispatches tool calls through the existing Stage 7 HTTP endpoints, keeping backend state authoritative;
- returns structured `function_call_output` events to OpenAI and asks the model to continue;
- displays compact AI tool-call diagnostics in the voice panel.

Stage 11 enables AI-driven registration state changes through voice, but the full registration conversation prompt and production call-center behavior remain deferred to later stages.

For manual UI testing, run the API first, then run the frontend and open `http://127.0.0.1:5173`.

Build and test:

```powershell
npm.cmd run build
npm.cmd test -- --run
```

## Configuration

Current local configuration contains OpenAI Realtime settings and session timeout settings:

```json
{
  "OpenAI": {
    "ApiKey": "",
    "BaseUrl": "https://api.openai.com/v1",
    "RealtimeModel": "gpt-realtime-2.1",
    "RealtimeVoice": "marin",
    "RealtimeInputTranscriptionModel": "gpt-realtime-whisper",
    "RealtimeMaxSdpOfferCharacters": 131072,
    "RealtimeCallsPerMinute": 12,
    "RealtimeInstructions": "You are a helpful HFU voice registration assistant demo. Keep responses brief. Use the provided registration tools when you need to save, confirm, inspect, clear, or complete registration data. Do not claim registration data was saved or completed unless a tool result confirms it."
  },
  "Frontend": {
    "BaseUrl": "http://localhost:5173"
  },
  "ConversationSessions": {
    "IncompleteSessionExpiration": "00:30:00",
    "CompletedSessionExpiration": "01:00:00",
    "CleanupInterval": "00:05:00"
  }
}
```

For local voice testing, set `OpenAI:ApiKey` in `appsettings.Development.json` or provide `OpenAI__ApiKey` as an environment variable. Do not put the API key in the React app or frontend environment variables. The Realtime call endpoint is rate-limited per remote address and session; tune it with `OpenAI__RealtimeCallsPerMinute`.

## Current Exclusions

These are intentionally not implemented yet:

- full registration system prompt
- production voice registration dialog policy
- SIP/IP telephony transport
- EF Core, database packages, Redis/backplane, or persistent storage
- production HFU integration
