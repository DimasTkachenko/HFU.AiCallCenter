# Stage 1 Solution Skeleton Design

## Status

Approved by the user on 2026-07-22 in the Codex task for `C:\git\Hfu.VoiceRegistration`.

## Goal

Create the Stage 1 skeleton for `Hfu.VoiceRegistration`: a runnable .NET backend, React/Vite frontend, tests, and documentation, without implementing registration logic or AI integrations.

## Architecture

The backend uses a small layered .NET 8 solution:

- `Domain`: future pure domain model, no external dependencies.
- `Application`: application contracts and service registration, references Domain.
- `Infrastructure`: future external adapters and infrastructure registrations, references Application.
- `Api`: ASP.NET Core Web API host, references Application and Infrastructure.

The only backend behavior in Stage 1 is `GET /health`, returning a simple JSON payload for frontend consumption. `Program.cs` stays small by delegating layer registration to extension methods.

The frontend is a React + TypeScript + Vite app in `src/Hfu.VoiceRegistration.Web`. It contains a typed health API client and a first screen named `HFU Voice Registration Demo` with loading, healthy, and error states.

## Constraints

- Target `.NET 8`.
- Do not modify `C:\git\HFU\backend`.
- Do not add OpenAI SDK, Realtime API, WebRTC, SignalR, backend tools, fake HFU registration, EF Core, database packages, Redis packages, or persistent storage.
- Do not add real secrets.
- Use placeholders for future `OpenAI` configuration only.
- Keep the PoC warning visible in README: this is not intended for real personal data.

## Testing

- Domain and Application test projects exist and contain minimal smoke tests proving assembly setup.
- API integration tests verify `GET /health` returns HTTP 200 and `status = "healthy"`.
- Frontend build verifies TypeScript/Vite wiring.

## Acceptance Criteria

- All .NET projects are in the solution.
- Backend builds.
- .NET tests pass.
- Frontend installs/builds.
- `GET /health` is implemented and documented.
- README includes setup/build/test/run instructions and future-stage exclusions.
