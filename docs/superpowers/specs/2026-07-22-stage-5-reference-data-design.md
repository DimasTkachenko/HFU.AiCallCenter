# Stage 5 Reference Data Design

## Status

Approved by the user on 2026-07-22.

User decisions:

- canonical region names must be Ukrainian;
- ambiguous and not-found region values should be persisted as `NeedsClarification` with structured tool codes and suggestions when available.

## Goal

Implement application-level region reference data and resolver integration for backend registration tools without adding HTTP endpoints, OpenAI, WebRTC, SignalR, fake HFU registration, database persistence, Redis, EF Core, or production HFU integration.

## Architecture

`Hfu.VoiceRegistration.Application.ReferenceData` owns the in-memory Ukrainian region catalog, `IRegionReferenceDataProvider`, and `IRegionResolver`.

`RegistrationToolService` uses `IRegionResolver` when handling `currentRegion` and `regionBeforeWar`. The AI/model continues to send plain text; the backend performs matching and stores the server-owned result.

## Region Matching

The catalog stores:

- server-owned internal ID;
- Ukrainian canonical name;
- Ukrainian aliases;
- Russian aliases.

The resolver normalizes case, spaces, punctuation, and common Cyrillic variants. It checks exact aliases first, then conservative fuzzy matches. Internal region IDs are never indexed as aliases.

## Tool Behavior

Resolved region:

- store Ukrainian canonical name in the `RegistrationDraft`;
- store server-owned `ReferenceId`;
- status remains `Captured` until explicitly confirmed.

Ambiguous or not found:

- store the raw value;
- mark the field `NeedsClarification`;
- return `RegionAmbiguous` or `RegionNotFound`;
- include Ukrainian suggestions for ambiguous matches.

Hard invalid values outside region resolution still keep Stage 4 all-or-nothing behavior.

## Boundaries

Stage 5 does not add `GET /api/reference-data/regions`; that belongs to the backend HTTP API stage. It also does not add `complete_registration`, fake HFU registration, OpenAI, Realtime/WebRTC, SignalR, databases, Redis, EF Core, or production HFU integration.

## Testing

Application tests cover:

- Ukrainian/Russian alias matching;
- Ukrainian canonical display names;
- ambiguous Kyiv matching;
- generated region IDs not accepted as aliases;
- integration with `update_registration_fields`;
- ambiguity/not-found persisted as clarification.
