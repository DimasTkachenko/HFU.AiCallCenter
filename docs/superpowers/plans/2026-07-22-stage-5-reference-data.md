# Stage 5 Reference Data Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Ukrainian region reference data and integrate region resolution into backend registration tools.

**Architecture:** `Application.ReferenceData` owns the in-memory region catalog and resolver. `RegistrationToolService` consumes the resolver for region fields and keeps all mutation authority server-side through `IConversationSessionStore`.

**Tech Stack:** .NET 8, C#, xUnit, Microsoft.Extensions.DependencyInjection.

## Global Constraints

- Do not modify `C:\git\HFU\backend`.
- Canonical region names must be Ukrainian.
- Ambiguous and not-found region values must be persisted as `NeedsClarification`.
- Do not accept model-generated region IDs as aliases.
- Do not add HTTP endpoints for Stage 5.
- Do not add OpenAI SDK, Realtime API, WebRTC, SignalR, fake HFU registration, EF Core, database packages, Redis packages, or persistent storage.
- Keep all Stage 5 behavior testable without HTTP and OpenAI.

---

### Task 1: Reference Data Resolver Tests

**Files:**
- Create: `tests/Hfu.VoiceRegistration.Application.Tests/ReferenceData/RegionResolverTests.cs`

**Interfaces:**
- Consumes planned `RegionResolver`, `UkrainianRegionReferenceDataProvider`, `RegionResolutionStatus`.
- Produces failing tests for alias matching, ambiguous matching, and generated ID rejection.

- [x] Write failing tests for Ukrainian/Russian aliases.
- [x] Write failing tests for ambiguous Kyiv suggestions.
- [x] Write failing test proving internal IDs are not accepted as aliases.
- [x] Run `dotnet test tests\Hfu.VoiceRegistration.Application.Tests\Hfu.VoiceRegistration.Application.Tests.csproj` and verify RED on missing ReferenceData types.

### Task 2: Region Reference Data And Resolver

**Files:**
- Create: `src/Hfu.VoiceRegistration.Application/ReferenceData/IRegionReferenceDataProvider.cs`
- Create: `src/Hfu.VoiceRegistration.Application/ReferenceData/IRegionResolver.cs`
- Create: `src/Hfu.VoiceRegistration.Application/ReferenceData/RegionReferenceItem.cs`
- Create: `src/Hfu.VoiceRegistration.Application/ReferenceData/RegionResolutionResult.cs`
- Create: `src/Hfu.VoiceRegistration.Application/ReferenceData/RegionResolutionStatus.cs`
- Create: `src/Hfu.VoiceRegistration.Application/ReferenceData/RegionResolver.cs`
- Create: `src/Hfu.VoiceRegistration.Application/ReferenceData/UkrainianRegionReferenceDataProvider.cs`

**Interfaces:**
- Produces the application-level catalog and resolver.

- [x] Add Ukrainian canonical region data.
- [x] Add Ukrainian and Russian aliases.
- [x] Implement exact matching first.
- [x] Implement conservative fuzzy matching for ambiguous/not-found handling.
- [x] Do not index internal IDs as aliases.

### Task 3: Tool Integration

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationToolService.cs`
- Modify: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationToolError.cs`
- Modify: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationToolErrorCodes.cs`
- Modify: `src/Hfu.VoiceRegistration.Application/RegistrationTools/RegistrationFieldSnapshot.cs`
- Modify: `src/Hfu.VoiceRegistration.Domain/Registration/RegistrationField.cs`
- Modify: `tests/Hfu.VoiceRegistration.Application.Tests/RegistrationTools/RegistrationToolServiceTests.cs`

**Interfaces:**
- Consumes `IRegionResolver`.
- Produces region-aware `update_registration_fields` behavior.

- [x] Add `ReferenceId` to registration fields and snapshots.
- [x] Add `Suggestions` to tool errors.
- [x] Add `RegionAmbiguous` and `RegionNotFound` codes.
- [x] Resolve `currentRegion` and `regionBeforeWar`.
- [x] Persist ambiguous/not-found region fields as `NeedsClarification`.
- [x] Preserve hard invalid all-or-nothing behavior.
- [x] Run Application tests and verify GREEN.

### Task 4: DI And Documentation

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Application/DependencyInjection.cs`
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `PROJECT_CONTEXT.md`

**Interfaces:**
- Produces application DI registration and documented Stage 5 boundary.

- [x] Register `IRegionReferenceDataProvider`.
- [x] Register `IRegionResolver`.
- [x] Document Ukrainian canonical names and clarification behavior.
- [x] Document HTTP endpoint deferral.

### Task 5: Verification

**Files:**
- No code files.

**Interfaces:**
- Verifies the solution remains healthy.

- [x] Run `dotnet build Hfu.VoiceRegistration.sln`.
- [x] Run `dotnet test Hfu.VoiceRegistration.sln`.
- [x] Run frontend regression checks: `npm.cmd test -- --run` and `npm.cmd run build`.
- [x] Commit with message `feat: add stage 5 reference data`.
