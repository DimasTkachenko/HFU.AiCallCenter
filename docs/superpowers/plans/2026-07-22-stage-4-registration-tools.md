# Stage 4 Backend Registration Tools Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add application-level backend registration tool handlers for direct server-side draft updates.

**Architecture:** `Application` owns `IRegistrationToolService`, tool request/result DTOs, field registry, normalization, validation, and state snapshots. The service mutates sessions only through `IConversationSessionStore`.

**Tech Stack:** .NET 8, C#, xUnit, Microsoft.Extensions.DependencyInjection.

## Global Constraints

- Do not modify `C:\git\HFU\backend`.
- Do not add HTTP endpoints for Stage 4.
- Do not add OpenAI SDK, Realtime API, WebRTC, SignalR, fake HFU registration, EF Core, database packages, Redis packages, or persistent storage.
- Do not implement final `complete_registration` submission in Stage 4.
- Keep all Stage 4 behavior testable without HTTP and OpenAI.

---

### Task 1: Tool Service Behavior Tests

**Files:**
- Create: `tests/Hfu.VoiceRegistration.Application.Tests/RegistrationTools/RegistrationToolServiceTests.cs`

**Interfaces:**
- Consumes planned `RegistrationToolService`, `RegistrationFieldUpdate`, `RegistrationToolResult`.
- Produces failing tests for the application-level tool handlers.

- [x] Write failing tests for update, validation errors, confirm, clarification, clear, state retrieval, and unknown sessions.
- [x] Run `dotnet test tests\Hfu.VoiceRegistration.Application.Tests\Hfu.VoiceRegistration.Application.Tests.csproj` and verify RED on missing tool types.

### Task 2: Tool Contracts And State DTOs

**Files:**
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationTools/IRegistrationToolService.cs`
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationFieldUpdate.cs`
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationToolResult.cs`
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationToolError.cs`
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationToolErrorCodes.cs`
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationFieldSnapshot.cs`
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationStateSnapshot.cs`

**Interfaces:**
- Produces public application contracts for future HTTP and OpenAI adapters.

- [x] Add the public service interface.
- [x] Add tool input, result, error, field snapshot, and state snapshot records.

### Task 3: Tool Handler Implementation

**Files:**
- Create: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationToolService.cs`
- Modify: `src/Hfu.VoiceRegistration.Domain/Registration/RegistrationField.cs`

**Interfaces:**
- Consumes `IConversationSessionStore`, `RegistrationDraft`, `RegistrationCompletionValidator`.
- Produces direct backend handlers for update, confirm, clarification, clear, and get state.

- [x] Implement field registry for all known registration fields.
- [x] Implement typed conversion and basic normalization.
- [x] Reject unknown or invalid fields without mutating the draft.
- [x] Mutate sessions through `IConversationSessionStore.UpdateAsync`.
- [x] Preserve clarification reason on registration fields.
- [x] Return state snapshots with completion eligibility.
- [x] Run Application tests and verify GREEN.

### Task 4: DI And Documentation

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Application/DependencyInjection.cs`
- Modify: `tests/Hfu.VoiceRegistration.Application.Tests/DependencyInjectionTests.cs`
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `PROJECT_CONTEXT.md`

**Interfaces:**
- Produces `IRegistrationToolService` registration through `AddApplication`.

- [x] Register `IRegistrationToolService`.
- [x] Add DI coverage.
- [x] Update documentation to mark Stage 4 implemented and completion deferred.

### Task 5: Verification

**Files:**
- No code files.

**Interfaces:**
- Verifies the solution remains healthy.

- [x] Run `dotnet build Hfu.VoiceRegistration.sln`.
- [x] Run `dotnet test Hfu.VoiceRegistration.sln`.
- [x] Run frontend regression checks: `npm.cmd test -- --run` and `npm.cmd run build`.
- [x] Commit with message `feat: add stage 4 registration tools`.
