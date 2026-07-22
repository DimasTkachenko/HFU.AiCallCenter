# Stage 9 SignalR Live Updates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add SignalR live updates to the Stage 8 UI while keeping HTTP as the authoritative state source.

**Architecture:** `Hfu.VoiceRegistration.Api` hosts `/hubs/conversation` and publishes lightweight typed events to per-session groups after existing HTTP actions mutate state. `Hfu.VoiceRegistration.Web` owns the SignalR browser connection, rejoins the current session after reconnect, displays live status/events, and refreshes full state through HTTP after each event.

**Tech Stack:** ASP.NET Core SignalR, typed hubs, React 19, TypeScript, Vite 7, `@microsoft/signalr`, xUnit, Vitest, Testing Library.

## Global Constraints

- Do not modify `C:\git\HFU\backend`.
- Do not add OpenAI SDK, Realtime API, WebRTC, microphone access, audio playback, transcript rendering, OpenAI tool-call bridge, Redis/backplane, EF Core, database packages, persistent storage, authentication, authorization, or production HFU integration.
- SignalR is not the source of truth; frontend must refresh full session state through HTTP.
- SignalR payloads are lightweight typed events, not full registration DTOs.
- UI labels remain Russian.
- Canonical region names remain Ukrainian.

---

### Task 1: Backend SignalR Hub Tests RED

**Files:**
- Modify: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/Hfu.VoiceRegistration.Api.IntegrationTests.csproj`
- Create: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/ConversationRealtimeHubTests.cs`

**Interfaces:**
- Consumes future hub path `/hubs/conversation`.
- Consumes future client event name `conversationEvent`.
- Produces failing tests for hub connection, session group join, and update event delivery.

- [ ] Add `Microsoft.AspNetCore.SignalR.Client` test package reference.
- [ ] Write `ConnectsToConversationHub` using `HubConnectionBuilder`.
- [ ] Write `JoinedSessionReceivesRegistrationStateChangedEventAfterUpdate`.
- [ ] Run `dotnet test Hfu.VoiceRegistration.sln` and verify RED because `/hubs/conversation` is not mapped yet.

### Task 2: Backend SignalR Hub GREEN

**Files:**
- Create: `src/Hfu.VoiceRegistration.Api/Realtime/ConversationRealtimeEventType.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Realtime/ConversationRealtimeEvent.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Realtime/IConversationRealtimeClient.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Realtime/ConversationHubGroups.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Realtime/ConversationHub.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Realtime/IConversationRealtimeNotifier.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Realtime/SignalRConversationRealtimeNotifier.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/Program.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/Endpoints/ConversationSessionEndpoints.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/Endpoints/RegistrationToolEndpoints.cs`

**Interfaces:**
- Produces `JoinSession(Guid sessionId)` and `LeaveSession(Guid sessionId)` hub methods.
- Produces `IConversationRealtimeClient.ConversationEvent(ConversationRealtimeEvent conversationEvent)`.
- Produces `IConversationRealtimeNotifier.NotifyAsync(Guid sessionId, long version, ConversationRealtimeEventType type, string message, CancellationToken cancellationToken, string? correlationId = null)`.

- [ ] Implement typed event records and enum.
- [ ] Implement group helper `ConversationHubGroups.ForSession(Guid sessionId)`.
- [ ] Implement `ConversationHub`.
- [ ] Register SignalR and notifier in `Program.cs`.
- [ ] Map `/hubs/conversation`.
- [ ] Publish events after create, abandon, update, confirm, mark clarification, clear, get state, and complete actions.
- [ ] Run backend SignalR tests and verify GREEN.

### Task 3: Frontend SignalR Client Tests RED

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Web/package.json`
- Modify: `src/Hfu.VoiceRegistration.Web/package-lock.json`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/realtimeTypes.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/conversationRealtimeClient.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/conversationRealtimeClient.test.ts`

**Interfaces:**
- Consumes `@microsoft/signalr`.
- Produces `createConversationRealtimeClient(options)` with `connect`, `joinSession`, `leaveSession`, `onEvent`, `onStatusChange`, and `stop`.

- [ ] Install `@microsoft/signalr`.
- [ ] Write failing tests with mocked SignalR builder.
- [ ] Verify hub URL is `/hubs/conversation` by default and uses `VITE_API_BASE_URL` when provided.
- [ ] Verify `JoinSession` and `LeaveSession` invoke matching hub methods.
- [ ] Verify `conversationEvent` handler forwards typed events.
- [ ] Run `npm.cmd test -- src/api/conversationRealtimeClient.test.ts --run` and verify RED.

### Task 4: Frontend SignalR Client GREEN

**Files:**
- Create: `src/Hfu.VoiceRegistration.Web/src/api/realtimeTypes.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/conversationRealtimeClient.ts`
- Modify: `src/Hfu.VoiceRegistration.Web/vite.config.ts`

**Interfaces:**
- Produces typed realtime client wrapper.
- Extends Vite proxy to `/hubs`.

- [ ] Implement frontend realtime event types.
- [ ] Implement SignalR wrapper with automatic reconnect.
- [ ] Implement status callbacks for connecting, connected, reconnecting, disconnected, and error.
- [ ] Add Vite proxy for `/hubs`.
- [ ] Run frontend realtime client tests and verify GREEN.

### Task 5: App Live Updates Tests RED

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Web/src/App.test.tsx`

**Interfaces:**
- Consumes `createConversationRealtimeClient`.
- Produces failing tests for UI live status, event list, join session, and HTTP refresh after live event.

- [ ] Mock `conversationRealtimeClient`.
- [ ] Write failing test that creating a session joins the SignalR session group and shows `live подключено`.
- [ ] Write failing test that receiving `RegistrationStateChanged` appends a developer event and calls `GET /api/conversation-sessions/{sessionId}`.
- [ ] Write failing test that restoring a session rejoins the session group.
- [ ] Run `npm.cmd test -- src/App.test.tsx --run` and verify RED.

### Task 6: App Live Updates GREEN

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Web/src/App.tsx`
- Modify: `src/Hfu.VoiceRegistration.Web/src/styles.css`

**Interfaces:**
- Consumes frontend realtime client wrapper.
- Produces visible live status and compact developer live event list.

- [ ] Add live connection state to `App`.
- [ ] Connect SignalR once per app lifecycle.
- [ ] Join current session after create/restore and leave previous session when switching.
- [ ] Rejoin and HTTP-refresh after reconnect.
- [ ] Append received events and refresh full state through `getConversationSession`.
- [ ] Display live status and recent event list with Russian labels.
- [ ] Run `npm.cmd test -- src/App.test.tsx --run` and verify GREEN.

### Task 7: Documentation, Verification, And Commit

**Files:**
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `PROJECT_CONTEXT.md`
- Modify: `docs/superpowers/plans/2026-07-22-stage-9-signalr-live-updates.md`

**Interfaces:**
- Documents Stage 9 behavior and manual testing.

- [ ] Update docs to mark Stage 9 implemented and OpenAI/WebRTC still deferred.
- [ ] Run `dotnet build Hfu.VoiceRegistration.sln`.
- [ ] Run `dotnet test Hfu.VoiceRegistration.sln`.
- [ ] Run `npm.cmd test -- --run`.
- [ ] Run `npm.cmd run build`.
- [ ] Start local API and frontend dev servers and visually verify live status/event refresh.
- [ ] Commit with message `feat: add stage 9 signalr live updates`.
