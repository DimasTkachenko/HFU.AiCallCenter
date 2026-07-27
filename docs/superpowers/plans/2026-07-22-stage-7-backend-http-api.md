# Stage 7 Backend HTTP API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose the backend registration flow through typed HTTP endpoints with Swagger/OpenAPI and integration-test coverage.

**Architecture:** `Api` owns HTTP contracts and endpoint mapping. `Application` remains the source of registration behavior. Business tool errors return structured `RegistrationToolResult` payloads; HTTP transport errors return Problem Details.

**Tech Stack:** .NET 8, ASP.NET Core minimal APIs, Swashbuckle.AspNetCore 6.6.2, xUnit integration tests.

## Global Constraints

- Do not modify `C:\git\HFU\backend`.
- Do not add OpenAI SDK, Realtime API, WebRTC, SignalR, EF Core, database packages, Redis packages, persistent storage, or production HFU integration.
- Use typed REST endpoints, not a generic JSON tool dispatcher.
- Do not accept final registration DTOs from HTTP clients.
- Business tool failures return structured tool result responses with state.
- Missing sessions return HTTP Problem Details.
- Full registration flow must be testable through Swagger/Postman without OpenAI.

---

### Task 1: API Integration Tests RED

**Files:**
- Create: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/ConversationSessionEndpointTests.cs`
- Create: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/RegistrationToolEndpointTests.cs`
- Create: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/ReferenceDataEndpointTests.cs`
- Create: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/SwaggerEndpointTests.cs`

**Interfaces:**
- Consumes planned HTTP endpoints under `/api/conversation-sessions`, `/api/reference-data/regions`, and `/swagger/v1/swagger.json`.
- Produces failing integration tests that drive API contracts.

- [x] Write failing tests for Swagger JSON availability.
- [x] Write failing tests for creating and reading sessions.
- [x] Write failing tests for missing session Problem Details.
- [x] Write failing tests for regions reference data endpoint.
- [x] Write failing tests for typed registration tool endpoints, including successful completion.
- [x] Write failing tests for business completion errors returning `200 OK`.
- [x] Write failing tests for abandoning sessions and completed-session conflict.
- [x] Run API integration tests and verify RED.

### Task 2: HTTP Contracts

**Files:**
- Create: `src/Hfu.VoiceRegistration.Api/Contracts/ConversationSessionResponse.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Contracts/ConversationEventResponse.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Contracts/RegistrationToolHttpContracts.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Contracts/ReferenceDataHttpContracts.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Contracts/ApiContractMapper.cs`

**Interfaces:**
- Consumes `ConversationSession`, `RegistrationToolResult`, and `RegionReferenceItem`.
- Produces HTTP DTOs used by endpoint mapping and tests.

- [x] Add session response and event response DTOs.
- [x] Add typed request DTOs for all registration tools.
- [x] Add region response DTO.
- [x] Add mapper methods from domain/application objects to HTTP DTOs.

### Task 3: Endpoint Mapping

**Files:**
- Create: `src/Hfu.VoiceRegistration.Api/Endpoints/ConversationSessionEndpoints.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Endpoints/RegistrationToolEndpoints.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Endpoints/ReferenceDataEndpoints.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/Program.cs`

**Interfaces:**
- Consumes `IConversationSessionStore`, `IRegistrationToolService`, `IRegionReferenceDataProvider`, and HTTP contracts.
- Produces mapped minimal API endpoints.

- [x] Map session create/get/abandon endpoints.
- [x] Map typed registration tool endpoints.
- [x] Map reference regions endpoint.
- [x] Add helper behavior for missing-session Problem Details.
- [x] Run API integration tests and verify endpoint GREEN except Swagger if package wiring remains pending.

### Task 4: Swagger/OpenAPI

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Api/Hfu.VoiceRegistration.Api.csproj`
- Modify: `src/Hfu.VoiceRegistration.Api/Program.cs`

**Interfaces:**
- Consumes mapped endpoints and HTTP contracts.
- Produces Swagger JSON and Swagger UI.

- [x] Add `Swashbuckle.AspNetCore` package reference version `6.6.2`.
- [x] Register `AddEndpointsApiExplorer` and `AddSwaggerGen`.
- [x] Map `UseSwagger` and `UseSwaggerUI`.
- [x] Run API integration tests and verify GREEN.

### Task 5: Documentation And Verification

**Files:**
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `PROJECT_CONTEXT.md`
- Modify: `docs/superpowers/plans/2026-07-22-stage-7-backend-http-api.md`

**Interfaces:**
- Documents Stage 7 scope and manual testing instructions.

- [x] Update docs to mark Stage 7 implemented and OpenAI/UI still deferred.
- [x] Run `dotnet build Hfu.VoiceRegistration.sln`.
- [x] Run `dotnet test Hfu.VoiceRegistration.sln`.
- [x] Run frontend regression checks: `npm.cmd test -- --run` and `npm.cmd run build`.
- [x] Commit with message `feat: add stage 7 backend http api`.
