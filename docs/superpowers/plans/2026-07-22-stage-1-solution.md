# Stage 1 Solution Skeleton Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Stage 1 `Hfu.VoiceRegistration` solution skeleton with backend, frontend, tests, and documentation.

**Architecture:** A .NET 8 layered backend exposes only `GET /health` for Stage 1. A React + TypeScript + Vite frontend calls that endpoint through a typed client and displays health state. Docs explain local setup and preserve the future architecture boundaries.

**Tech Stack:** .NET 8, ASP.NET Core Web API, xUnit, Microsoft.AspNetCore.Mvc.Testing, React, TypeScript, Vite.

## Global Constraints

- Do not modify `C:\git\HFU\backend`.
- Target `.NET 8`.
- Do not add OpenAI SDK, Realtime API, WebRTC, SignalR, backend tools, fake HFU registration, EF Core, database packages, Redis packages, or persistent storage.
- Do not add real secrets.
- Use `npm.cmd` instead of bare `npm` in PowerShell.
- Keep `Program.cs` small with DI extension methods.

---

### Task 1: Backend Solution And Layer Skeleton

**Files:**
- Create: `Hfu.VoiceRegistration.sln`
- Create: `src/Hfu.VoiceRegistration.Domain/Hfu.VoiceRegistration.Domain.csproj`
- Create: `src/Hfu.VoiceRegistration.Application/Hfu.VoiceRegistration.Application.csproj`
- Create: `src/Hfu.VoiceRegistration.Infrastructure/Hfu.VoiceRegistration.Infrastructure.csproj`
- Create: `src/Hfu.VoiceRegistration.Api/Hfu.VoiceRegistration.Api.csproj`
- Create: `src/Hfu.VoiceRegistration.Application/DependencyInjection.cs`
- Create: `src/Hfu.VoiceRegistration.Infrastructure/DependencyInjection.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Program.cs`
- Create: `src/Hfu.VoiceRegistration.Api/appsettings.json`
- Create: `src/Hfu.VoiceRegistration.Api/appsettings.Development.json`

**Interfaces:**
- Produces: `AddApplication(IServiceCollection)` and `AddInfrastructure(IServiceCollection, IConfiguration)` extension methods.
- Produces: `Program` partial class for integration testing.

- [ ] Create solution and project files targeting `net8.0`.
- [ ] Add project references: Application -> Domain, Infrastructure -> Application, Api -> Application and Infrastructure.
- [ ] Add small DI extension methods.
- [ ] Add a minimal API host with service registration and JSON configuration placeholders.

### Task 2: Health Endpoint TDD

**Files:**
- Create: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/Hfu.VoiceRegistration.Api.IntegrationTests.csproj`
- Create: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/HealthEndpointTests.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/Program.cs`

**Interfaces:**
- Consumes: API `Program` partial class.
- Produces: `GET /health` returning JSON with `status`, `service`, `timestampUtc`, and `version`.

- [ ] Write failing integration test expecting `GET /health` to return HTTP 200 and `status = "healthy"`.
- [ ] Run the integration test and verify it fails before endpoint implementation.
- [ ] Implement the minimal `/health` endpoint.
- [ ] Run the integration test and verify it passes.

### Task 3: Test Project Smoke Coverage

**Files:**
- Create: `tests/Hfu.VoiceRegistration.Domain.Tests/Hfu.VoiceRegistration.Domain.Tests.csproj`
- Create: `tests/Hfu.VoiceRegistration.Domain.Tests/AssemblySmokeTests.cs`
- Create: `tests/Hfu.VoiceRegistration.Application.Tests/Hfu.VoiceRegistration.Application.Tests.csproj`
- Create: `tests/Hfu.VoiceRegistration.Application.Tests/DependencyInjectionTests.cs`

**Interfaces:**
- Consumes: `AddApplication(IServiceCollection)`.
- Produces: baseline test coverage for solution wiring.

- [ ] Add Domain smoke test.
- [ ] Add Application DI test.
- [ ] Add test projects to solution.
- [ ] Run all .NET tests.

### Task 4: Frontend Skeleton

**Files:**
- Create: `src/Hfu.VoiceRegistration.Web/package.json`
- Create: `src/Hfu.VoiceRegistration.Web/index.html`
- Create: `src/Hfu.VoiceRegistration.Web/tsconfig.json`
- Create: `src/Hfu.VoiceRegistration.Web/tsconfig.node.json`
- Create: `src/Hfu.VoiceRegistration.Web/vite.config.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/main.tsx`
- Create: `src/Hfu.VoiceRegistration.Web/src/App.tsx`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/healthClient.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/styles.css`

**Interfaces:**
- Consumes: backend `GET /health`.
- Produces: `fetchHealth(baseUrl?: string): Promise<HealthResponse>`.

- [ ] Create Vite React TypeScript app files.
- [ ] Implement typed health client.
- [ ] Implement first screen with loading, healthy, and error states.
- [ ] Install dependencies with `npm.cmd install`.
- [ ] Run frontend build.

### Task 5: Documentation And Repository Hygiene

**Files:**
- Create: `.gitignore`
- Create: `README.md`
- Create: `docs/architecture.md`

**Interfaces:**
- Consumes: final project paths and commands.
- Produces: human-readable setup and architecture notes.

- [ ] Add `.gitignore` for .NET, Node, IDE, and local secret files.
- [ ] Add README with prerequisites, build/test/run commands, health endpoint, and Stage 1 exclusions.
- [ ] Add architecture document describing backend/frontend/OpenAI boundaries and future SIP-ready transport direction.
- [ ] Run final verification: `dotnet build`, `dotnet test`, `npm.cmd run build`.
