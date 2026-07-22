# Stage 6 Fake HFU Registration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add backend completion workflow and fake HFU registration for validated server-owned drafts.

**Architecture:** `Application` owns the completion tool, final DTO mapper, and fake HFU contracts. `Infrastructure` owns in-memory fake registration ID generation and fake HFU adapter. No HTTP endpoint is added in this stage.

**Tech Stack:** .NET 8, C#, xUnit, Microsoft.Extensions.DependencyInjection.

## Global Constraints

- Do not modify `C:\git\HFU\backend`.
- Do not add HTTP endpoints for Stage 6.
- Do not add OpenAI SDK, Realtime API, WebRTC, SignalR, EF Core, database packages, Redis packages, or persistent storage.
- Do not accept final registration DTOs from the model/tool caller.
- `complete_registration` accepts only `personalDataConsent` and `registrationConfirmed`.
- Repeated completion must not generate a new ID and must return `RegistrationAlreadyCompleted`.
- Demo registration IDs use `DEMO-{year}-{counter:000000}`.
- Keep all Stage 6 behavior testable without HTTP and OpenAI.

---

### Task 1: Completion Workflow Tests

**Files:**
- Modify: `tests/Hfu.VoiceRegistration.Application.Tests/RegistrationTools/RegistrationToolServiceTests.cs`
- Create: `tests/Hfu.VoiceRegistration.Application.Tests/RegistrationCompletion/FinalRegistrationDtoMapperTests.cs`

**Interfaces:**
- Consumes planned `CompleteRegistrationAsync`, `CompleteRegistrationRequest`, `IFakeHfuRegistrationService`, and final DTO mapper.
- Produces failing tests for completion behavior and final DTO mapping.

- [x] Write failing tests for successful completion.
- [x] Write failing tests for validation failure.
- [x] Write failing tests for already completed sessions.
- [x] Write failing tests for final DTO mapping.
- [x] Run Application tests and verify RED.

### Task 2: Application Completion Contracts And Mapper

**Files:**
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationCompletion/CompleteRegistrationRequest.cs`
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationCompletion/FinalRegistrationDto.cs`
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationCompletion/FakeHfuRegistrationResponse.cs`
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationCompletion/IFakeHfuRegistrationService.cs`
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationCompletion/FinalRegistrationDtoMapper.cs`
- Modify: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationToolResult.cs`
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationCompletionDetails.cs`

**Interfaces:**
- Produces completion contracts and final DTO mapping.

- [x] Add completion request and fake HFU contracts.
- [x] Add final DTO record.
- [x] Implement final DTO mapper from `RegistrationDraft`.
- [x] Add completion details to tool result.

### Task 3: complete_registration Tool Workflow

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Application/RegistrationTools/IRegistrationToolService.cs`
- Modify: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationToolService.cs`
- Modify: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationToolErrorCodes.cs`

**Interfaces:**
- Consumes `IConversationSessionStore`, `IFakeHfuRegistrationService`, and `FinalRegistrationDtoMapper`.
- Produces `CompleteRegistrationAsync`.

- [x] Add `RegistrationAlreadyCompleted`.
- [x] Add `RegistrationCannotBeCompleted`.
- [x] Persist completion flags before validation result.
- [x] Validate the full draft before fake HFU submission.
- [x] Mark valid sessions completed and store `RegistrationResult`.
- [x] Return existing result/state on repeated completion.
- [x] Run Application tests and verify GREEN.

### Task 4: Infrastructure Fake HFU Adapter

**Files:**
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationCompletion/IRegistrationIdGenerator.cs`
- Create: `src/Hfu.VoiceRegistration.Infrastructure/RegistrationCompletion/InMemoryDemoRegistrationIdGenerator.cs`
- Create: `src/Hfu.VoiceRegistration.Infrastructure/RegistrationCompletion/FakeHfuRegistrationService.cs`
- Modify: `src/Hfu.VoiceRegistration.Infrastructure/DependencyInjection.cs`
- Create: `tests/Hfu.VoiceRegistration.Infrastructure.Tests/RegistrationCompletion/InMemoryDemoRegistrationIdGeneratorTests.cs`
- Create: `tests/Hfu.VoiceRegistration.Infrastructure.Tests/RegistrationCompletion/FakeHfuRegistrationServiceTests.cs`

**Interfaces:**
- Produces fake HFU adapter and ID generator.

- [x] Implement in-memory ID generator.
- [x] Implement fake HFU registration service.
- [x] Register infrastructure services.
- [x] Run Infrastructure tests and verify GREEN.

### Task 5: Documentation And Verification

**Files:**
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `PROJECT_CONTEXT.md`

**Interfaces:**
- Documents Stage 6 scope and exclusions.

- [x] Update docs to mark Stage 6 implemented and HTTP still deferred.
- [x] Run `dotnet build Hfu.VoiceRegistration.sln`.
- [x] Run `dotnet test Hfu.VoiceRegistration.sln`.
- [x] Run frontend regression checks: `npm.cmd test -- --run` and `npm.cmd run build`.
- [x] Commit with message `feat: add stage 6 fake hfu registration`.
