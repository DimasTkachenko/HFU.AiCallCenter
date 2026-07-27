# Stage 2 Domain Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement pure domain models and completion validation for Stage 2.

**Architecture:** Domain types live only in `Hfu.VoiceRegistration.Domain`; tests live in `Hfu.VoiceRegistration.Domain.Tests`. Completion validation is a pure static domain service that returns a structured validation result. Conversation session is a domain concept only; persistence and concurrency stay for later stages.

**Tech Stack:** .NET 8, C#, xUnit.

## Global Constraints

- Do not modify `C:\git\HFU\backend`.
- Do not add HTTP endpoints for Stage 2.
- Do not add OpenAI SDK, Realtime API, WebRTC, SignalR, backend tools, fake HFU registration, EF Core, database packages, Redis packages, or persistent storage.
- Keep all Stage 2 business logic testable without HTTP and OpenAI.
- Follow the user-approved conservative completion rule.

---

### Task 1: Registration Field And Draft Model

**Files:**
- Create: `src/Hfu.VoiceRegistration.Domain/Registration/RegistrationFieldStatus.cs`
- Create: `src/Hfu.VoiceRegistration.Domain/Registration/RegistrationField.cs`
- Create: `src/Hfu.VoiceRegistration.Domain/Registration/UserCategory.cs`
- Create: `src/Hfu.VoiceRegistration.Domain/Registration/RegistrationDraft.cs`
- Create: `tests/Hfu.VoiceRegistration.Domain.Tests/Registration/RegistrationDraftTests.cs`

**Interfaces:**
- Produces: `RegistrationField<T>`, `RegistrationFieldStatus`, `UserCategory`, `RegistrationDraft`.

- [ ] Write failing tests for field defaults, captured/confirmed/rejected helpers, draft initialization, and user category values.
- [ ] Run `dotnet test tests\Hfu.VoiceRegistration.Domain.Tests\Hfu.VoiceRegistration.Domain.Tests.csproj` and verify failure.
- [ ] Implement minimal domain types.
- [ ] Run the same test project and verify pass.

### Task 2: Completion Validation

**Files:**
- Create: `src/Hfu.VoiceRegistration.Domain/Registration/RegistrationValidationIssue.cs`
- Create: `src/Hfu.VoiceRegistration.Domain/Registration/RegistrationValidationResult.cs`
- Create: `src/Hfu.VoiceRegistration.Domain/Registration/RegistrationCompletionValidator.cs`
- Create: `tests/Hfu.VoiceRegistration.Domain.Tests/Registration/RegistrationCompletionValidatorTests.cs`

**Interfaces:**
- Consumes: `RegistrationDraft`, `RegistrationField<T>`, `UserCategory`.
- Produces: `RegistrationCompletionValidator.Evaluate(RegistrationDraft draft): RegistrationValidationResult`.

- [ ] Write failing tests for the conservative completion rule.
- [ ] Run the domain test project and verify failure.
- [ ] Implement validation result and completion validator.
- [ ] Run the domain test project and verify pass.

### Task 3: Conversation Session Domain Concept

**Files:**
- Create: `src/Hfu.VoiceRegistration.Domain/Conversations/ConversationSessionStatus.cs`
- Create: `src/Hfu.VoiceRegistration.Domain/Conversations/ConversationSession.cs`
- Create: `src/Hfu.VoiceRegistration.Domain/Conversations/ConversationEvent.cs`
- Create: `src/Hfu.VoiceRegistration.Domain/Conversations/RegistrationResult.cs`
- Create: `tests/Hfu.VoiceRegistration.Domain.Tests/Conversations/ConversationSessionTests.cs`

**Interfaces:**
- Consumes: `RegistrationDraft`.
- Produces: `ConversationSession.Create(DateTimeOffset now)`.

- [ ] Write failing tests for session initialization and event recording.
- [ ] Run the domain test project and verify failure.
- [ ] Implement minimal session domain types.
- [ ] Run the domain test project and verify pass.

### Task 4: Documentation And Verification

**Files:**
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `PROJECT_CONTEXT.md`

**Interfaces:**
- Consumes: implemented domain model and validation scope.
- Produces: updated Stage 2 documentation.

- [ ] Update docs to mark Stage 2 as implemented and Stage 3 as next.
- [ ] Run `dotnet build Hfu.VoiceRegistration.sln`.
- [ ] Run `dotnet test Hfu.VoiceRegistration.sln`.
- [ ] Run frontend verification to confirm Stage 1 surface was not broken: `npm.cmd test -- --run` and `npm.cmd run build`.
- [ ] Commit with message `feat: add stage 2 domain model`.
