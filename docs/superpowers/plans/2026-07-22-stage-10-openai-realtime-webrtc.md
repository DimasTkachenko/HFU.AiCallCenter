# Stage 10 OpenAI Realtime WebRTC Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add OpenAI Realtime WebRTC voice conversation to the Stage 9 UI without registration tool calls.

**Architecture:** The frontend owns browser WebRTC, microphone, remote audio playback, and the Realtime data channel. The backend owns the permanent OpenAI API key and proxies browser SDP offers to OpenAI `POST /v1/realtime/calls`, returning SDP answers.

**Tech Stack:** ASP.NET Core Minimal APIs, `HttpClientFactory`, React 19, TypeScript, Vite 7, browser WebRTC APIs, xUnit, Vitest, Testing Library.

## Global Constraints

- Use the OpenAI Realtime unified WebRTC interface.
- Keep `OpenAI:ApiKey` server-side only; never expose it through the frontend.
- Local config may use `appsettings.json` or `appsettings.Development.json`; server deployment can override with env vars.
- Default Realtime model is `gpt-realtime-2.1`.
- Default voice is `marin`.
- Do not add registration tool calls, the OpenAI tool-call bridge, or the full registration system prompt in Stage 10.
- Do not add SIP, Redis/backplane, EF Core, database packages, persistent storage, authentication, authorization, or production HFU integration.

---

### Task 1: Backend Realtime Contract And Tests RED

**Files:**
- Create: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/OpenAIRealtimeClientTests.cs`
- Create: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/OpenAIRealtimeEndpointTests.cs`

**Interfaces:**
- Future service: `IOpenAIRealtimeClient.CreateCallAsync(string sdpOffer, string safetyIdentifier, CancellationToken cancellationToken)`.
- Future endpoint: `POST /api/conversation-sessions/{sessionId}/realtime/calls`.

- [ ] Add failing tests for OpenAI request formatting: authorization header, `OpenAI-Safety-Identifier`, multipart `sdp`, multipart `session`, default model `gpt-realtime-2.1`, default voice `marin`.
- [ ] Add failing endpoint tests for successful SDP answer, missing session `404`, blank SDP `400`, completed session `409`, and missing API key `500`.
- [ ] Run `dotnet test Hfu.VoiceRegistration.sln` and verify RED.

### Task 2: Backend Realtime Endpoint GREEN

**Files:**
- Create: `src/Hfu.VoiceRegistration.Api/OpenAIRealtime/OpenAIRealtimeOptions.cs`
- Create: `src/Hfu.VoiceRegistration.Api/OpenAIRealtime/OpenAIRealtimeCallResult.cs`
- Create: `src/Hfu.VoiceRegistration.Api/OpenAIRealtime/IOpenAIRealtimeClient.cs`
- Create: `src/Hfu.VoiceRegistration.Api/OpenAIRealtime/OpenAIRealtimeClient.cs`
- Create: `src/Hfu.VoiceRegistration.Api/OpenAIRealtime/OpenAIRealtimeExceptions.cs`
- Create: `src/Hfu.VoiceRegistration.Api/Endpoints/OpenAIRealtimeEndpoints.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/Endpoints/ApiProblemDetails.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/Program.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/appsettings.json`
- Modify: `src/Hfu.VoiceRegistration.Api/appsettings.Development.json`

**Interfaces:**
- Produces `OpenAIRealtimeOptions` bound to `OpenAI`.
- Produces `IOpenAIRealtimeClient`.
- Produces `MapOpenAIRealtimeEndpoints()`.

- [ ] Implement options with safe defaults and blank-value fallback.
- [ ] Implement `OpenAIRealtimeClient` using `HttpClient` and multipart form data.
- [ ] Implement Problem Details helpers for realtime config/API/session status failures.
- [ ] Implement `/api/conversation-sessions/{sessionId}/realtime/calls`.
- [ ] Register options, typed HttpClient, and endpoint mapping in `Program.cs`.
- [ ] Run backend tests and verify GREEN.

### Task 3: Frontend Realtime API And WebRTC Client RED

**Files:**
- Create: `src/Hfu.VoiceRegistration.Web/src/api/openAIRealtimeClient.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/openAIRealtimeClient.test.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/openAIRealtimeTypes.ts`

**Interfaces:**
- Produces `createOpenAIRealtimeWebRtcClient(options)`.
- Produces `startOpenAIRealtimeCall(sessionId, sdpOffer, baseUrl?)`.

- [ ] Add failing test that raw SDP is posted to `/api/conversation-sessions/{sessionId}/realtime/calls` with `Content-Type: application/sdp`.
- [ ] Add failing test that WebRTC start requests microphone, creates `oai-events`, posts offer, and sets remote answer.
- [ ] Add failing test that Realtime data channel events become transcript entries.
- [ ] Add failing test that stop closes data channel, peer connection, and microphone tracks.
- [ ] Run `npm.cmd test -- src/api/openAIRealtimeClient.test.ts --run` and verify RED.

### Task 4: Frontend Realtime API And WebRTC Client GREEN

**Files:**
- Create: `src/Hfu.VoiceRegistration.Web/src/api/openAIRealtimeClient.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/openAIRealtimeTypes.ts`

**Interfaces:**
- Produces reusable frontend voice client for App.

- [ ] Implement raw SDP HTTP client and Problem Details parsing.
- [ ] Implement WebRTC lifecycle wrapper.
- [ ] Implement transcript parsing for `conversation.item.input_audio_transcription.completed`, `response.audio_transcript.delta`, and `response.audio_transcript.done`.
- [ ] Implement cleanup on stop and failed start.
- [ ] Run frontend client tests and verify GREEN.

### Task 5: App Voice UI RED/GREEN

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Web/src/App.test.tsx`
- Modify: `src/Hfu.VoiceRegistration.Web/src/App.tsx`
- Modify: `src/Hfu.VoiceRegistration.Web/src/styles.css`

**Interfaces:**
- Consumes `createOpenAIRealtimeWebRtcClient`.
- Produces voice panel controls and transcript display.

- [ ] Add failing tests for disabled voice controls before session creation.
- [ ] Add failing tests that starting voice creates the WebRTC client for current session and shows `голос подключён`.
- [ ] Add failing tests that stopping voice calls `stop()` and shows stopped state.
- [ ] Add failing tests that transcript entries render in the voice panel.
- [ ] Implement voice state, client lifecycle, Russian UI labels, and transcript panel.
- [ ] Run `npm.cmd test -- src/App.test.tsx --run` and verify GREEN.

### Task 6: Documentation, Verification, Manual Smoke, Commit

**Files:**
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `PROJECT_CONTEXT.md`
- Modify: `docs/superpowers/plans/2026-07-22-stage-10-openai-realtime-webrtc.md`

**Interfaces:**
- Documents Stage 10 behavior and OpenAI config.

- [ ] Update docs to mark Stage 10 implemented and Stage 11/12 deferred.
- [ ] Run `dotnet build Hfu.VoiceRegistration.sln`.
- [ ] Run `dotnet test Hfu.VoiceRegistration.sln`.
- [ ] Run `npm.cmd test -- --run`.
- [ ] Run `npm.cmd run build`.
- [ ] Start local API and frontend. Visually verify session creation, voice panel, and safe error state when no API key is configured.
- [ ] If an API key is configured locally, manually verify microphone permission, WebRTC connection, remote audio, and transcripts.
- [ ] Commit with message `feat: add stage 10 openai realtime webrtc`.
