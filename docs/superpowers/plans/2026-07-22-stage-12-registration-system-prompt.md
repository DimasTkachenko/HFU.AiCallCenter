# Stage 12 Registration System Prompt Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a backend-owned Stage 12 OpenAI Realtime registration interview prompt for the live voice assistant.

**Architecture:** A focused backend prompt class owns the versioned default instructions. `OpenAIRealtimeOptions` keeps the existing appsettings/environment override path, and `OpenAIRealtimeClient` continues sending `options.EffectiveRealtimeInstructions` in the Realtime session payload.

**Tech Stack:** ASP.NET Core, `System.Text.Json`, xUnit, OpenAI Realtime session payload, React/Vite documentation-only updates.

## Global Constraints

- The assistant speaks to users only in Ukrainian.
- User replies may be Ukrainian, Russian, or mixed Ukrainian/Russian.
- Backend registration state remains authoritative; the model never edits `RegistrationDraft` directly.
- The model must use registration tools for save, confirm, clarify, state, clear, and completion actions.
- `complete_registration` may be called only after current state check, final summary, explicit personal data consent, and explicit final confirmation.
- Do not add reconnect/recovery hardening, developer prompt panel UI, prompt eval automation, SIP/IP telephony, authentication, authorization, EF Core, Redis/backplane, persistent storage, or production HFU integration.

---

### Task 1: Backend Prompt Contract

**Files:**
- Modify: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/OpenAIRealtimeClientTests.cs`
- Create: `src/Hfu.VoiceRegistration.Api/OpenAIRealtime/OpenAIRealtimeRegistrationPrompt.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/OpenAIRealtime/OpenAIRealtimeOptions.cs`

- [x] Add failing tests that default instructions contain the Stage 12 prompt version, Ukrainian-only assistant output rule, mixed-language user input rule, required fields, confirmation fields, tool policy, and completion gate.
- [x] Add a failing test that a custom `RealtimeInstructions` override still wins.
- [x] Implement `OpenAIRealtimeRegistrationPrompt.CurrentInstructions`.
- [x] Point `OpenAIRealtimeOptions` default instructions at the prompt class.
- [x] Run backend prompt tests and verify green.

### Task 2: Session Payload And Config Defaults

**Files:**
- Modify: `tests/Hfu.VoiceRegistration.Api.IntegrationTests/OpenAIRealtimeClientTests.cs`
- Modify: `src/Hfu.VoiceRegistration.Api/appsettings.json`
- Modify: `src/Hfu.VoiceRegistration.Api/appsettings.Development.json`

- [x] Add a failing session-payload assertion for the prompt version and Ukrainian interview rule.
- [x] Keep all six function tools and `tool_choice: "auto"` in the session payload.
- [x] Leave checked-in `OpenAI:RealtimeInstructions` blank so the versioned prompt is used by default.
- [x] Run `dotnet test Hfu.VoiceRegistration.sln` and verify green.

### Task 3: Documentation And Manual Test Guide

**Files:**
- Modify: `README.md`
- Modify: `PROJECT_CONTEXT.md`
- Modify: `docs/architecture.md`

- [x] Document Stage 12 behavior and how it differs from Stage 11.
- [x] Document manual Ukrainian voice-interview testing with demo data and real `OpenAI:ApiKey`.
- [x] Remove Stage 12 from current exclusions while keeping later-stage exclusions.
- [x] Run full backend and frontend verification commands.
