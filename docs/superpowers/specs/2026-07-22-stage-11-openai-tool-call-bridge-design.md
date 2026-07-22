# Stage 11 OpenAI Tool-Call Bridge Design

Approved by the user on 2026-07-22 with the recommended scope: expose all existing registration tools to OpenAI Realtime, including `complete_registration`, while keeping backend validation authoritative.

## Goal

Connect OpenAI Realtime function calls to the existing Stage 7 registration tool HTTP endpoints so a live voice session can update, confirm, inspect, clear, and complete a registration through backend-owned business rules.

## Architecture

The backend adds function tool definitions to the Realtime session configuration sent to OpenAI. The browser still owns the WebRTC data channel. A focused frontend bridge listens for Realtime function-call events, dispatches them to the existing `registrationClient`, applies returned `RegistrationToolResult` state to the UI, and sends a `function_call_output` item back to OpenAI before asking the model to continue.

The bridge is transport-aware but business-rule-light. It parses tool names and JSON arguments, maps them to typed HTTP client calls, and formats structured success/error output for OpenAI. It does not mutate registration state directly.

## Tools

- `update_registration_fields`
- `confirm_registration_fields`
- `mark_fields_for_clarification`
- `clear_registration_fields`
- `get_registration_state`
- `complete_registration`

`complete_registration` is available in Stage 11, but it remains guarded by the existing backend `personalDataConsent`, `registrationConfirmed`, validation, and `RegistrationAlreadyCompleted` behavior.

## UI

The existing voice panel gains compact AI tool activity diagnostics: latest tool calls, running/completed/error status, and error messages. Existing registration state and tool feedback panels remain authoritative.

## Error Handling

Malformed Realtime events, unknown tools, and invalid tool arguments return structured tool-call output to OpenAI without calling the backend. HTTP/Problem Details failures are returned as structured errors. Backend business errors remain normal `RegistrationToolResult` responses and are displayed in the existing tool feedback panel.

## Testing

Backend integration tests verify that the OpenAI Realtime session payload includes all tool definitions and `tool_choice: "auto"`. Frontend tests cover function-call event parsing, tool dispatch, OpenAI output events, unknown tools, invalid arguments, and App UI tool activity.

## Exclusions

Stage 11 does not add the full registration system prompt, production HFU integration, SIP/IP telephony, authentication/authorization, Redis/backplane, EF Core, database packages, or persistent storage.
