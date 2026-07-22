# Stage 3 In-Memory Session Management Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add application-level session store contracts and infrastructure-level in-memory session management.

**Architecture:** `Application` owns the `IConversationSessionStore` abstraction and options. `Infrastructure` owns `InMemoryConversationSessionStore` and cleanup hosted service. `Infrastructure.Tests` verifies storage, locking, versioning, expiration, and DI wiring.

**Tech Stack:** .NET 8, C#, xUnit, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting.

## Global Constraints

- Do not modify `C:\git\HFU\backend`.
- Do not add HTTP endpoints for Stage 3.
- Do not add OpenAI SDK, Realtime API, WebRTC, SignalR, backend tools, fake HFU registration, EF Core, database packages, Redis packages, or persistent storage.
- Keep all Stage 3 behavior testable without HTTP and OpenAI.
- Defaults: incomplete inactive session expiration 30 minutes, completed session expiration 60 minutes, cleanup interval 5 minutes.

---

### Task 1: Infrastructure Test Project And Store Contract Tests

**Files:**
- Create: `tests/Hfu.VoiceRegistration.Infrastructure.Tests/Hfu.VoiceRegistration.Infrastructure.Tests.csproj`
- Create: `tests/Hfu.VoiceRegistration.Infrastructure.Tests/Conversations/InMemoryConversationSessionStoreTests.cs`
- Modify: `Hfu.VoiceRegistration.sln`

**Interfaces:**
- Consumes planned `IConversationSessionStore`, `InMemoryConversationSessionStore`, `ConversationSessionStoreOptions`.
- Produces failing tests for storage behavior.

- [ ] Create xUnit Infrastructure tests project.
- [ ] Add references to Infrastructure and Application.
- [ ] Add project to solution.
- [ ] Write failing tests for create/get/remove, duplicate create, unknown update, mutation versioning, concurrent mutation locking, unfinished expiration, and completed expiration.
- [ ] Run the new test project and verify failure.

### Task 2: Application Store Contracts

**Files:**
- Create: `src/Hfu.VoiceRegistration.Application/Conversations/IConversationSessionStore.cs`
- Create: `src/Hfu.VoiceRegistration.Application/Conversations/ConversationSessionStoreOptions.cs`

**Interfaces:**
- Produces `IConversationSessionStore` and `ConversationSessionStoreOptions`.

- [ ] Add application contracts.
- [ ] Run Infrastructure tests and verify they still fail because implementation is missing.

### Task 3: In-Memory Store Implementation

**Files:**
- Create: `src/Hfu.VoiceRegistration.Infrastructure/Conversations/InMemoryConversationSessionStore.cs`

**Interfaces:**
- Consumes `IConversationSessionStore`, `ConversationSessionStoreOptions`, `ConversationSession`.
- Produces working in-memory store.

- [ ] Implement `ConcurrentDictionary<Guid, StoredConversationSession>`.
- [ ] Implement session-level `SemaphoreSlim`.
- [ ] Implement create, get, update, remove.
- [ ] Implement mutation update with store-owned version increment.
- [ ] Implement cleanup expiration.
- [ ] Run Infrastructure tests and verify pass.

### Task 4: Cleanup Service And DI

**Files:**
- Create: `src/Hfu.VoiceRegistration.Infrastructure/Conversations/ConversationSessionCleanupService.cs`
- Modify: `src/Hfu.VoiceRegistration.Infrastructure/DependencyInjection.cs`
- Create: `tests/Hfu.VoiceRegistration.Infrastructure.Tests/DependencyInjectionTests.cs`

**Interfaces:**
- Consumes `IConversationSessionStore`, `IOptions<ConversationSessionStoreOptions>`.
- Produces DI registration for store, options, `TimeProvider.System`, and hosted cleanup service.

- [ ] Write failing DI test proving `AddInfrastructure` registers the session store.
- [ ] Implement cleanup service and DI registration.
- [ ] Run Infrastructure tests and verify pass.

### Task 5: Documentation And Verification

**Files:**
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `PROJECT_CONTEXT.md`

**Interfaces:**
- Consumes implemented Stage 3 scope.
- Produces updated documentation.

- [ ] Update docs to mark Stage 3 as implemented and Stage 4 as next.
- [ ] Run `dotnet build Hfu.VoiceRegistration.sln`.
- [ ] Run `dotnet test Hfu.VoiceRegistration.sln`.
- [ ] Run frontend regression checks: `npm.cmd test -- --run` and `npm.cmd run build`.
- [ ] Commit with message `feat: add stage 3 in-memory sessions`.
