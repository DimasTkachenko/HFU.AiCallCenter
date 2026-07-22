# Stage 8 React UI Without Voice Design

## Status

Approved by the user on 2026-07-22.

User decisions:

- build the recommended hybrid demo/developer UI;
- visible UI text should be Russian;
- canonical region names remain Ukrainian as defined by Stage 5 reference data;
- no OpenAI, Realtime/WebRTC, or SignalR in this stage.

## Goal

Make the registration flow testable through the browser UI without OpenAI Realtime, WebRTC, SignalR, persistent storage, or production HFU integration.

## Architecture

`Hfu.VoiceRegistration.Web` becomes a single-page operational demo UI over the Stage 7 backend HTTP API. The frontend owns only presentation state, local form state, session restoration, and HTTP calls. The backend remains the source of truth for registration draft state, validation, completion eligibility, and fake HFU result generation.

The UI is intentionally hybrid: it has a human-readable registration form for demo flow and a compact developer action panel that exposes backend tool behavior. It is not a final end-user wizard and not a raw JSON console.

## User Experience

The first screen shows backend health, the current session state, and primary session actions:

- create a new session;
- restore an existing session from `localStorage`;
- refresh state;
- abandon the current session.

The registration workspace contains:

- a registration form with known fields;
- region selects populated from `GET /api/reference-data/regions`;
- user category select;
- consent and final confirmation checkboxes;
- a demo-data fill action for fast manual testing;
- tool actions for update, confirm, mark clarification, clear, get state, and complete registration.

The state area shows:

- session metadata;
- all backend field snapshots with status, value, raw value, reference ID, and clarification reason;
- missing required fields;
- fields awaiting confirmation;
- fields requiring clarification;
- completion issues;
- structured tool errors;
- fake HFU registration result when completed.

## Data Flow

On load:

1. Fetch `/health`.
2. Fetch `/api/reference-data/regions`.
3. Read `hfu.voiceRegistration.sessionId` from `localStorage`.
4. If present, request `/api/conversation-sessions/{sessionId}` and display the restored state.
5. If restore fails with `404`, clear the stored session ID and show the Problem Details message.

Actions:

- create session calls `POST /api/conversation-sessions` and stores the returned `sessionId`;
- update fields sends non-empty form values to `update-registration-fields`;
- confirm fields sends field names currently selected for confirmation;
- mark clarification and clear use comma-separated field-name inputs;
- complete registration sends only `personalDataConsent` and `registrationConfirmed`;
- every successful tool response replaces the displayed registration state.

## Error Handling

HTTP Problem Details appear in an error panel with status, title, and detail. Business errors from `RegistrationToolResult.Errors` remain visible in the tool errors panel alongside the current state.

Network failures are shown as readable frontend errors. The UI should not claim a registration succeeded unless the backend returns a completion result.

## Boundaries

Stage 8 does not add OpenAI SDK, Realtime API, WebRTC, SignalR, audio capture, transcript UI, server-side rendering, routing, EF Core, databases, Redis, or production HFU integration.

## Testing

Frontend tests cover:

- Stage 7 API client request paths and error parsing;
- session creation and `localStorage` persistence;
- restoration of a saved session;
- reference data rendering with Ukrainian region names;
- update/confirm/complete flow through the UI;
- business tool errors displayed as structured errors.

Manual testing uses the backend dev server and the Vite dev server.
