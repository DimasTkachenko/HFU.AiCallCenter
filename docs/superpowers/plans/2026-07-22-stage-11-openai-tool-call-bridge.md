# Stage 11 OpenAI Tool-Call Bridge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let OpenAI Realtime voice sessions call the existing backend registration tools.

**Architecture:** Backend configures Realtime function tools in the session payload. Frontend parses function-call events from the WebRTC data channel, dispatches them through the existing typed HTTP registration client, updates UI state, and returns `function_call_output` to OpenAI.

**Tech Stack:** ASP.NET Core Minimal APIs, `HttpClientFactory`, React 19, TypeScript, Vite 7, browser WebRTC data channels, xUnit, Vitest, Testing Library.

## Global Constraints

- Keep `OpenAI:ApiKey` server-side only; never expose it through the frontend.
- Use OpenAI Realtime function tools for app-owned registration business logic.
- Keep backend registration state authoritative; the model never edits `RegistrationDraft` directly.
- Expose `complete_registration` in Stage 11, guarded by existing backend validation.
- Do not add the full registration system prompt in Stage 11.
- Do not add SIP, Redis/backplane, EF Core, database packages, persistent storage, authentication, authorization, or production HFU integration.

---

### Task 1: Backend Tool Definitions

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Api/OpenAIRealtime/OpenAIRealtimeClient.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/OpenAIRealtime/OpenAIRealtimeOptions.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/appsettings.json`
- Modify: `src/Hfu.VoiceRegistration.Api/appsettings.Development.json`
- Modify: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/OpenAIRealtimeClientTests.cs`

- [x] Add a failing test that the OpenAI session payload includes all six function tools and `tool_choice: "auto"`.
- [x] Implement Realtime function tool definitions with JSON schemas.
- [x] Update default instructions so they no longer say registration tools are disconnected.
- [x] Run backend tests and verify green.

### Task 2: Realtime Tool-Call Parsing

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Web/src/api/openAIRealtimeTypes.ts`
- Modify: `src/Hfu.VoiceRegistration.Web/src/api/openAIRealtimeClient.ts`
- Modify: `src/Hfu.VoiceRegistration.Web/src/api/openAIRealtimeClient.test.ts`

- [x] Add a failing test for `response.function_call_arguments.done`.
- [x] Add typed `OpenAIRealtimeToolCall` emission.
- [x] Support function calls embedded in `response.output_item.done`.
- [x] Run frontend tests and verify green.

### Task 3: Frontend Tool Bridge

**Files:**
- Create: `src/Hfu.VoiceRegistration.Web/src/api/openAIRealtimeToolBridge.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/openAIRealtimeToolBridge.test.ts`

- [x] Add failing tests for successful tool dispatch and `function_call_output` emission.
- [x] Add failing tests for unknown tool and invalid JSON arguments.
- [x] Implement bridge dispatch for all six registration tools.
- [x] Run bridge tests and verify green.

### Task 4: App Integration And Diagnostics

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Web/src/App.tsx`
- Modify: `src/Hfu.VoiceRegistration.Web/src/App.test.tsx`
- Modify: `src/Hfu.VoiceRegistration.Web/src/styles.css`

- [x] Add failing App test that a voice tool call updates backend state and shows tool activity.
- [x] Wire bridge lifecycle to start/stop voice.
- [x] Render compact AI tool-call diagnostics in the voice panel.
- [x] Run App tests and verify green.

### Task 5: Documentation And Verification

**Files:**
- Modify: `README.md`

- [x] Document Stage 11 capabilities and manual visual testing.
- [x] Run `dotnet build Hfu.VoiceRegistration.sln`.
- [x] Run `dotnet test Hfu.VoiceRegistration.sln`.
- [x] Run `npm.cmd test -- --run`.
- [x] Run `npm.cmd run build`.
