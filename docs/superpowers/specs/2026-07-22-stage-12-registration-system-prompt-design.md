# Stage 12 Registration System Prompt Design

Approved by the user on 2026-07-22 with the recommended hybrid flow and a strict language requirement: the assistant speaks Ukrainian, while accepting Ukrainian, Russian, and mixed Ukrainian/Russian user replies.

## Goal

Add a production-shaped registration interview prompt to the backend-owned OpenAI Realtime session configuration so a live voice assistant can conduct the HFU demo interview, use existing registration tools, fill the backend draft, confirm critical values, and complete registration only through backend validation.

## Architecture

The prompt is versioned in backend code as the default `OpenAIRealtimeOptions` instructions. `OpenAI:RealtimeInstructions` remains an appsettings/environment override, but the checked-in appsettings files leave it blank so the versioned Stage 12 prompt is used by default.

The prompt does not add business logic. It teaches the model how to sequence conversation and when to call the Stage 11 function tools. Backend registration tools remain authoritative for field names, value parsing, region normalization, validation, state, and completion.

## Interview Behavior

The assistant must speak to the user only in Ukrainian. It may understand Russian and mixed Ukrainian/Russian replies, normalize values for tools, and keep the conversation calm, short, and focused.

The interview flow is:

- greet and identify the HFU demo registration context;
- warn that local PoC demos must not use real personal data;
- call `get_registration_state` before collecting data;
- ask one short question at a time, unless the user volunteers several fields;
- save confident values with `update_registration_fields`;
- mark ambiguous or incomplete values with `mark_fields_for_clarification`;
- explicitly confirm critical/exact fields before completion;
- read current state before the final summary;
- ask for personal data consent and final registration confirmation;
- call `complete_registration` only after explicit consent and final confirmation.

## Fields And Canonical Values

The prompt names all backend registration fields and the completion requirements already enforced by the domain validator.

Critical/exact fields requiring explicit confirmation are:

- `phoneNumber`
- `dateOfBirth`
- `currentRegion`
- `currentCity`
- `userCategory`
- `email` when provided

The model must save `dateOfBirth` as `yyyy-MM-dd`, save regions as user-facing region names that the backend can resolve to Ukrainian canonical names, and save `userCategory` as one of the backend enum values: `InternallyDisplacedPerson`, `HasManyChildren`, `DisabledPerson`, `MilitaryPerson`, `MilitaryPersonRelative`, or `Other`.

For `InternallyDisplacedPerson`, the prompt must collect `regionBeforeWar` and `displacedCertificateYear`.

## Error Handling

Tool results are authoritative. The assistant must not claim a field was saved, confirmed, or completed until the corresponding tool result succeeds. If a tool returns validation errors, it asks a targeted follow-up in Ukrainian and uses suggestions from the tool result when present.

## Testing

Backend tests verify that default options use the Stage 12 prompt, that appsettings overrides still work, and that the OpenAI Realtime session payload includes the versioned prompt plus all Stage 11 function tools.

Manual testing requires a real `OpenAI:ApiKey`, API server, browser UI, microphone permission, and a demo conversation with non-real personal data.

## Exclusions

Stage 12 does not add reconnect/recovery hardening, developer prompt panel UI, prompt eval automation, SIP/IP telephony, authentication/authorization, EF Core, Redis/backplane, persistent storage, or production HFU integration.
