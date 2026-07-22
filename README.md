# Hfu.VoiceRegistration

External advertising PoC for HFU voice-assisted registration. Stage 1 builds only the runnable project skeleton: .NET backend, React/Vite frontend, tests, documentation, and a health endpoint.

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

Stage 1 includes placeholder configuration only:

```json
{
  "OpenAI": {
    "ApiKey": "",
    "RealtimeModel": ""
  },
  "Frontend": {
    "BaseUrl": "http://localhost:5173"
  }
}
```

No OpenAI API key is required for Stage 1.

## Stage 1 Exclusions

These are intentionally not implemented yet:

- registration domain model and validation rules
- fake HFU registration
- SignalR
- OpenAI client or Realtime API
- WebRTC
- backend AI tools
- EF Core, database packages, Redis, or persistent storage
- production HFU integration

Do not move to Stage 2 without a separate request.
