# Stage 9 SignalR Live Updates Design

## Status

Approved by the user on 2026-07-22.

User decisions:

- use the recommended lightweight SignalR event model;
- SignalR is not the source of truth;
- frontend refreshes complete session state through HTTP after live events;
- include typed event kinds for future transcript and tool-call stages, but do not generate OpenAI/WebRTC events yet.

## Goal

Add live backend-to-frontend updates for the existing Stage 8 manual registration UI without adding OpenAI, WebRTC, audio capture, transcript UI, Redis, persistent storage, or production HFU integration.

## Architecture

`Hfu.VoiceRegistration.Api` hosts a SignalR hub at `/hubs/conversation`. Frontend clients join one session group at a time by calling `JoinSession(sessionId)` and leave it with `LeaveSession(sessionId)`.

The API layer owns SignalR publishing. Application and Domain projects stay transport-agnostic. Existing HTTP endpoints continue to execute registration actions; after successful state-changing actions, endpoint code publishes a lightweight `ConversationRealtimeEvent` to the session group.

The React app owns the browser connection. It displays connection state and, when it receives an event for the current session, calls `GET /api/conversation-sessions/{sessionId}` to refresh the complete authoritative state.

## Event Contract

The event sent to clients is:

- `eventId: string`
- `sessionId: string`
- `version: number`
- `type: ConversationRealtimeEventType`
- `message: string`
- `occurredAtUtc: string`
- `correlationId?: string | null`

Stage 9 publishes these real event types:

- `SessionCreated`
- `SessionUpdated`
- `RegistrationStateChanged`
- `RegistrationToolCompleted`
- `RegistrationCompleted`
- `SessionAbandoned`
- `DiagnosticEventAdded`

The enum also reserves typed values for later stages:

- `TranscriptReceived`
- `ToolCallReceived`
- `ToolCallCompleted`
- `ValidationFailed`
- `ConnectionStatusChanged`

## Frontend Behavior

The UI adds a live connection indicator in the existing session/diagnostics area:

- `подключение`
- `live подключено`
- `reconnect`
- `live отключено`
- readable error message when connection setup fails.

When a session is created or restored, the frontend opens the SignalR connection if necessary and joins that session group. When the current session changes, it leaves the previous group and joins the next one. On reconnect, it rejoins the current session group and refreshes session state through HTTP.

When the frontend receives the typed hub event `ConversationEvent`, it appends the event to a compact developer event list and refreshes full session state through HTTP. The UI must not trust SignalR payloads as full state.

## Error Handling

Unknown or missing sessions are handled by existing HTTP restore and action errors. SignalR connection failures are shown as live-connection errors but do not block manual HTTP actions.

If state refresh after an event fails, the existing Problem Details panel displays the HTTP failure. The event remains visible in the developer list so the operator can see that live delivery happened.

## Boundaries

Stage 9 does not add OpenAI SDK, Realtime API, WebRTC, microphone access, audio playback, transcript rendering, OpenAI tool-call bridge, Redis/backplane, EF Core, database packages, persistent storage, authentication, authorization, or production HFU integration.

## Testing

Backend tests cover:

- hub endpoint accepts SignalR client connections;
- clients can join a session group;
- state-changing HTTP actions publish `ConversationEvent` messages to the joined session group;
- events include session ID, version, type, and message.

Frontend tests cover:

- SignalR client wrapper builds the expected hub URL and invokes `JoinSession`/`LeaveSession`;
- App connects after creating/restoring a session;
- App refreshes session state through HTTP after a live event;
- live connection state and developer events are visible.
