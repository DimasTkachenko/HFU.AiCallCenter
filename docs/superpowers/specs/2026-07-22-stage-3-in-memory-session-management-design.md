# Stage 3 In-Memory Session Management Design

## Status

Approved by the user on 2026-07-22. The user also approved adding a dedicated `Hfu.VoiceRegistration.Infrastructure.Tests` project.

## Goal

Implement reliable in-memory storage for multiple independent `ConversationSession` instances without adding HTTP APIs, OpenAI, SignalR, WebRTC, backend registration tools, fake HFU registration, databases, Redis, or production HFU integration.

## Architecture

The session store contract belongs to `Hfu.VoiceRegistration.Application` because application code will depend on it in later stages.

The in-memory implementation belongs to `Hfu.VoiceRegistration.Infrastructure` because it is an implementation detail that can later be replaced by database, Redis, or another persistence adapter.

## Components

- `IConversationSessionStore`: async CRUD plus mutation and cleanup operations.
- `ConversationSessionStoreOptions`: timeout settings from the specification.
- `TimeProvider` usage for deterministic expiration tests.
- `InMemoryConversationSessionStore`: `ConcurrentDictionary<Guid, StoredSession>` plus session-level `SemaphoreSlim`.
- `ConversationSessionCleanupService`: hosted service shell that periodically calls cleanup.

## Store Behavior

- `CreateAsync` stores a new session and rejects duplicate IDs.
- `GetAsync` returns the stored session or `null`.
- `UpdateAsync` replaces an existing session and rejects unknown IDs.
- `RemoveAsync` removes a session if present.
- `UpdateAsync(sessionId, mutate, cancellationToken)` runs one mutation at a time per session and persists the returned session.
- Successful mutation increases `Version` before storing, so callers do not need each domain method to remember version increments.
- Events remain part of the stored `ConversationSession`.
- Cleanup removes:
  - unfinished sessions inactive longer than `IncompleteSessionExpiration`;
  - completed sessions inactive longer than `CompletedSessionExpiration`.

## Defaults

- Incomplete inactive session expiration: 30 minutes.
- Completed session expiration: 60 minutes.
- Cleanup interval: 5 minutes.

## Testing

`Hfu.VoiceRegistration.Infrastructure.Tests` covers:

- create/get/remove;
- duplicate create rejection;
- unknown update rejection;
- mutation versioning;
- session-level locking under concurrent mutations;
- unfinished expiration;
- completed expiration;
- cleanup service registration through DI.

## Boundaries

Stage 3 does not expose new HTTP endpoints and does not change the frontend. Manual UI behavior remains the Stage 1 health screen.
