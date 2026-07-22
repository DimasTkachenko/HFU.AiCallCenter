import { describe, expect, it, vi } from "vitest";
import {
  completeRegistration,
  confirmRegistrationFields,
  createConversationSession,
  fetchRegions,
  getConversationSession,
  updateRegistrationFields
} from "./registrationClient";

describe("registrationClient", () => {
  it("creates a conversation session through the Stage 7 API", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({
        sessionId: "11111111-1111-1111-1111-111111111111",
        status: "Created",
        version: 0,
        createdAt: "2026-07-22T12:00:00Z",
        lastActivityAt: "2026-07-22T12:00:00Z",
        realtimeConnectionId: null,
        registrationResult: null,
        state: emptyState("11111111-1111-1111-1111-111111111111"),
        events: []
      }, 201)
    );
    vi.stubGlobal("fetch", fetchMock);

    const session = await createConversationSession("http://localhost:5076");

    expect(fetchMock).toHaveBeenCalledWith("http://localhost:5076/api/conversation-sessions", {
      method: "POST",
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      body: "{}"
    });
    expect(session.sessionId).toBe("11111111-1111-1111-1111-111111111111");
    expect(session.status).toBe("Created");
  });

  it("reads an existing conversation session", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({
        sessionId: "22222222-2222-2222-2222-222222222222",
        status: "Active",
        version: 3,
        createdAt: "2026-07-22T12:00:00Z",
        lastActivityAt: "2026-07-22T12:03:00Z",
        realtimeConnectionId: null,
        registrationResult: null,
        state: emptyState("22222222-2222-2222-2222-222222222222"),
        events: []
      })
    );
    vi.stubGlobal("fetch", fetchMock);

    const session = await getConversationSession(
      "22222222-2222-2222-2222-222222222222",
      "http://localhost:5076/"
    );

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5076/api/conversation-sessions/22222222-2222-2222-2222-222222222222",
      { headers: { Accept: "application/json" } }
    );
    expect(session.version).toBe(3);
  });

  it("loads Ukrainian region reference data", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({
        regions: [
          {
            id: "hfu-region-kharkivska",
            name: "Харківська область",
            aliases: ["Харківська", "Харьковская область"]
          }
        ]
      })
    );
    vi.stubGlobal("fetch", fetchMock);

    const regions = await fetchRegions();

    expect(fetchMock).toHaveBeenCalledWith("/api/reference-data/regions", {
      headers: { Accept: "application/json" }
    });
    expect(regions[0].name).toBe("Харківська область");
  });

  it("calls typed registration tool endpoints", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse(toolResult()))
      .mockResolvedValueOnce(jsonResponse(toolResult()))
      .mockResolvedValueOnce(jsonResponse({
        ...toolResult(),
        completion: {
          finalRegistration: {
            firstName: "Dimas",
            lastName: "Tkachenko",
            patronymic: null,
            dateOfBirth: "1991-08-24",
            phoneNumber: "+380501112233",
            email: null,
            currentRegion: "Харківська область",
            currentRegionReferenceId: "hfu-region-kharkivska",
            currentCity: "Харків",
            actualAddress: null,
            userCategory: "Other",
            regionBeforeWar: null,
            regionBeforeWarReferenceId: null,
            displacedCertificateYear: null,
            personalDataConsent: true,
            registrationConfirmed: true
          },
          registrationResult: {
            registrationId: "DEMO-2026-000001",
            completedAt: "2026-07-22T12:05:00Z"
          }
        }
      }));
    vi.stubGlobal("fetch", fetchMock);

    await updateRegistrationFields("33333333-3333-3333-3333-333333333333", {
      fields: [{ name: "firstName", value: "Dimas" }]
    });
    await confirmRegistrationFields("33333333-3333-3333-3333-333333333333", {
      fieldNames: ["firstName"]
    });
    const complete = await completeRegistration("33333333-3333-3333-3333-333333333333", {
      personalDataConsent: true,
      registrationConfirmed: true
    });

    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      "/api/conversation-sessions/33333333-3333-3333-3333-333333333333/tools/update-registration-fields",
      {
        method: "POST",
        headers: { Accept: "application/json", "Content-Type": "application/json" },
        body: JSON.stringify({ fields: [{ name: "firstName", value: "Dimas" }] })
      }
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      "/api/conversation-sessions/33333333-3333-3333-3333-333333333333/tools/confirm-registration-fields",
      {
        method: "POST",
        headers: { Accept: "application/json", "Content-Type": "application/json" },
        body: JSON.stringify({ fieldNames: ["firstName"] })
      }
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      3,
      "/api/conversation-sessions/33333333-3333-3333-3333-333333333333/tools/complete-registration",
      {
        method: "POST",
        headers: { Accept: "application/json", "Content-Type": "application/json" },
        body: JSON.stringify({ personalDataConsent: true, registrationConfirmed: true })
      }
    );
    expect(complete.completion?.registrationResult.registrationId).toBe("DEMO-2026-000001");
  });

  it("throws parsed Problem Details for HTTP-layer errors", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        jsonResponse(
          {
            title: "Conversation session not found",
            status: 404,
            detail: "Conversation session was not found."
          },
          404,
          "application/problem+json"
        )
      )
    );

    await expect(getConversationSession("missing")).rejects.toMatchObject({
      title: "Conversation session not found",
      status: 404,
      detail: "Conversation session was not found."
    });
  });
});

function jsonResponse(body: unknown, status = 200, contentType = "application/json") {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": contentType }
  });
}

function emptyState(sessionId = "11111111-1111-1111-1111-111111111111") {
  return {
    sessionId,
    version: 0,
    fields: [],
    missingRequiredFields: [],
    fieldsRequiringClarification: [],
    fieldsAwaitingConfirmation: [],
    registrationCanBeCompleted: false,
    completionIssues: []
  };
}

function toolResult() {
  return {
    succeeded: true,
    state: emptyState("33333333-3333-3333-3333-333333333333"),
    errors: [],
    completion: null
  };
}
