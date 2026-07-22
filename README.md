# Hfu.VoiceRegistration

External advertising PoC for HFU voice-assisted registration. The current implementation contains the Stage 1 runnable skeleton, Stage 2 pure domain model, Stage 3 in-memory conversation session management, and Stage 4 application-level backend registration tools.

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

This stage does not add HTTP endpoints or OpenAI integration. The future tool-call bridge should call this application service instead of editing registration state directly.

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

The Vite dev server proxies `/health` to `http://localhost:5076`.

Build and test:

```powershell
npm.cmd run build
npm.cmd test -- --run
```

## Configuration

Current local configuration contains OpenAI placeholders and Stage 3 session timeout settings:

```json
{
  "OpenAI": {
    "ApiKey": "",
    "RealtimeModel": ""
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

No OpenAI API key is required yet.

## Current Exclusions

These are intentionally not implemented yet:

- fake HFU registration
- SignalR
- OpenAI client or Realtime API
- WebRTC
- OpenAI tool-call bridge
- final `complete_registration` submission flow
- EF Core, database packages, Redis, or persistent storage
- production HFU integration
