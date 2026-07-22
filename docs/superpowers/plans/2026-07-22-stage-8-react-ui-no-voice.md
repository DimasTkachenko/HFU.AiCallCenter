# Stage 8 React UI Without Voice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a browser UI that can exercise the full registration flow through Stage 7 HTTP endpoints without voice integration.

**Architecture:** The React app owns presentation, local form state, session restoration, and HTTP calls. The backend remains authoritative for registration state, validation, reference data, and fake HFU completion. The UI is a hybrid demo/developer tool with Russian labels and Ukrainian canonical region values.

**Tech Stack:** React 19, TypeScript, Vite 7, Vitest, Testing Library, ASP.NET Core Stage 7 API.

## Global Constraints

- Do not modify `C:\git\HFU\backend`.
- Do not add OpenAI SDK, Realtime API, WebRTC, SignalR, audio capture, transcript UI, EF Core, database packages, Redis packages, persistent storage, or production HFU integration.
- UI labels are Russian.
- Canonical region names remain Ukrainian.
- Use Stage 7 typed HTTP endpoints; do not add a generic JSON dispatcher.
- The frontend must never submit a final registration DTO.
- Keep frontend design operational and demo-focused, not a marketing landing page.

---

### Task 1: Frontend API Client Tests RED

**Files:**
- Create: `src/Hfu.VoiceRegistration.Web/src/api/registrationTypes.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/registrationClient.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/registrationClient.test.ts`
- Modify: `src/Hfu.VoiceRegistration.Web/vite.config.ts`

**Interfaces:**
- Consumes Stage 7 endpoints under `/api/conversation-sessions` and `/api/reference-data/regions`.
- Produces typed API functions for the React UI.

- [x] Write failing tests for `createConversationSession`.
- [x] Write failing tests for `getConversationSession`.
- [x] Write failing tests for `fetchRegions`.
- [x] Write failing tests for `updateRegistrationFields`, `confirmRegistrationFields`, and `completeRegistration`.
- [x] Write failing tests for Problem Details parsing.
- [x] Run frontend tests and verify RED.

### Task 2: Frontend API Client GREEN

**Files:**
- Create: `src/Hfu.VoiceRegistration.Web/src/api/registrationTypes.ts`
- Create: `src/Hfu.VoiceRegistration.Web/src/api/registrationClient.ts`
- Modify: `src/Hfu.VoiceRegistration.Web/vite.config.ts`

**Interfaces:**
- Produces `createConversationSession`, `getConversationSession`, `fetchRegions`, `updateRegistrationFields`, `confirmRegistrationFields`, `markFieldsForClarification`, `clearRegistrationFields`, `getRegistrationState`, `completeRegistration`, and `abandonConversationSession`.

- [x] Implement API types matching Stage 7 JSON contracts.
- [x] Implement request helpers and Problem Details parsing.
- [x] Add Vite proxy for `/api`.
- [x] Run frontend API client tests and verify GREEN.

### Task 3: App UI Tests RED

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Web/src/App.test.tsx`

**Interfaces:**
- Consumes API client behavior through fetch mocks.
- Produces failing tests for session creation, restoration, update/confirm/complete flow, and business error display.

- [x] Write failing test for creating a session and storing `sessionId`.
- [x] Write failing test for restoring saved session.
- [x] Write failing test for rendering Ukrainian region reference data.
- [x] Write failing test for update, confirm, and complete registration actions.
- [x] Write failing test for displaying structured tool errors.
- [x] Run frontend tests and verify RED.

### Task 4: App UI GREEN

**Files:**
- Modify: `src/Hfu.VoiceRegistration.Web/src/App.tsx`
- Modify: `src/Hfu.VoiceRegistration.Web/src/styles.css`

**Interfaces:**
- Consumes `registrationClient` functions and Stage 7 response types.
- Produces the Stage 8 hybrid demo/developer UI.

- [x] Implement session lifecycle state and `localStorage` restore.
- [x] Implement health and region loading.
- [x] Implement registration form and demo-data fill action.
- [x] Implement update, confirm, mark clarification, clear, get state, complete, and abandon actions.
- [x] Implement state, errors, issues, and result panels.
- [x] Style the UI as a dense operational workspace with responsive layout.
- [x] Run frontend tests and verify GREEN.

### Task 5: Documentation And Verification

**Files:**
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `PROJECT_CONTEXT.md`
- Modify: `docs/superpowers/plans/2026-07-22-stage-8-react-ui-no-voice.md`

**Interfaces:**
- Documents Stage 8 scope and manual testing instructions.

- [x] Update docs to mark Stage 8 implemented and voice/OpenAI still deferred.
- [x] Run `dotnet build Hfu.VoiceRegistration.sln`.
- [x] Run `dotnet test Hfu.VoiceRegistration.sln`.
- [x] Run `npm.cmd test -- --run`.
- [x] Run `npm.cmd run build`.
- [x] Start local API and frontend dev servers for manual visual testing.
- [x] Commit with message `feat: add stage 8 react ui without voice`.
