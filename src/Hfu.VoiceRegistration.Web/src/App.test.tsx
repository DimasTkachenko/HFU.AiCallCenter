import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import App from "./App";
import type {
  ConversationRealtimeEvent,
  RealtimeConnectionState
} from "./api/realtimeTypes";
import type {
  OpenAIRealtimeEventLogEntry,
  OpenAIRealtimeToolCall,
  OpenAIRealtimeTranscriptEntry,
  OpenAIRealtimeVoiceConnectionState
} from "./api/openAIRealtimeTypes";

const sessionId = "11111111-1111-1111-1111-111111111111";

const realtimeClientMock = vi.hoisted(() => {
  const eventHandlers = new Set<(conversationEvent: ConversationRealtimeEvent) => void>();
  const statusHandlers = new Set<(state: RealtimeConnectionState) => void>();
  const client = {
    connect: vi.fn(async () => {
      for (const handler of statusHandlers) {
        handler({ status: "connected" });
      }
    }),
    joinSession: vi.fn(async () => undefined),
    leaveSession: vi.fn(async () => undefined),
    onEvent: vi.fn((handler: (conversationEvent: ConversationRealtimeEvent) => void) => {
      eventHandlers.add(handler);

      return () => eventHandlers.delete(handler);
    }),
    onStatusChange: vi.fn((handler: (state: RealtimeConnectionState) => void) => {
      statusHandlers.add(handler);

      return () => statusHandlers.delete(handler);
    }),
    stop: vi.fn(async () => undefined)
  };

  return {
    client,
    createConversationRealtimeClient: vi.fn(() => client),
    emitEvent(conversationEvent: ConversationRealtimeEvent) {
      for (const handler of eventHandlers) {
        handler(conversationEvent);
      }
    },
    reset() {
      eventHandlers.clear();
      statusHandlers.clear();
      client.connect.mockClear();
      client.joinSession.mockClear();
      client.leaveSession.mockClear();
      client.onEvent.mockClear();
      client.onStatusChange.mockClear();
      client.stop.mockClear();
      this.createConversationRealtimeClient.mockClear();
    }
  };
});

const openAIRealtimeClientMock = vi.hoisted(() => {
  const stateHandlers = new Set<(state: OpenAIRealtimeVoiceConnectionState) => void>();
  const transcriptHandlers = new Set<(entry: OpenAIRealtimeTranscriptEntry) => void>();
  const eventHandlers = new Set<(event: OpenAIRealtimeEventLogEntry) => void>();
  const toolCallHandlers = new Set<(toolCall: OpenAIRealtimeToolCall) => void>();
  const client = {
    start: vi.fn(async () => {
      for (const handler of stateHandlers) {
        handler({ status: "connected" });
      }
    }),
    stop: vi.fn(),
    sendEvent: vi.fn(),
    onStateChange: vi.fn((handler: (state: OpenAIRealtimeVoiceConnectionState) => void) => {
      stateHandlers.add(handler);

      return () => stateHandlers.delete(handler);
    }),
    onTranscript: vi.fn((handler: (entry: OpenAIRealtimeTranscriptEntry) => void) => {
      transcriptHandlers.add(handler);

      return () => transcriptHandlers.delete(handler);
    }),
    onEvent: vi.fn((handler: (event: OpenAIRealtimeEventLogEntry) => void) => {
      eventHandlers.add(handler);

      return () => eventHandlers.delete(handler);
    }),
    onToolCall: vi.fn((handler: (toolCall: OpenAIRealtimeToolCall) => void) => {
      toolCallHandlers.add(handler);

      return () => toolCallHandlers.delete(handler);
    })
  };

  return {
    client,
    createOpenAIRealtimeWebRtcClient: vi.fn((_options?: unknown) => client),
    emitTranscript(entry: OpenAIRealtimeTranscriptEntry) {
      for (const handler of transcriptHandlers) {
        handler(entry);
      }
    },
    emitToolCall(toolCall: OpenAIRealtimeToolCall) {
      for (const handler of toolCallHandlers) {
        handler(toolCall);
      }
    },
    reset() {
      stateHandlers.clear();
      transcriptHandlers.clear();
      eventHandlers.clear();
      toolCallHandlers.clear();
      client.start.mockClear();
      client.stop.mockClear();
      client.sendEvent.mockClear();
      client.onStateChange.mockClear();
      client.onTranscript.mockClear();
      client.onEvent.mockClear();
      client.onToolCall.mockClear();
      this.createOpenAIRealtimeWebRtcClient.mockClear();
    }
  };
});

vi.mock("./api/conversationRealtimeClient", () => ({
  createConversationRealtimeClient: realtimeClientMock.createConversationRealtimeClient
}));

vi.mock("./api/openAIRealtimeClient", () => ({
  createOpenAIRealtimeWebRtcClient: openAIRealtimeClientMock.createOpenAIRealtimeWebRtcClient
}));

vi.mock("./api/voiceAssistantClient", () => ({
  createVoiceAssistantClient: vi.fn((options) =>
    openAIRealtimeClientMock.createOpenAIRealtimeWebRtcClient(options)),
  getEffectiveProvider: vi.fn(() => "openai")
}));

describe("App", () => {
  afterEach(() => {
    cleanup();
    localStorage.clear();
    vi.unstubAllGlobals();
    realtimeClientMock.reset();
    openAIRealtimeClientMock.reset();
  });

  it("creates a session and stores it for refresh recovery", async () => {
    vi.stubGlobal(
      "fetch",
      createFetchMock({
        "POST /api/conversation-sessions": sessionResponse()
      })
    );

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "Создать сессию" }));

    expect(await screen.findByText(sessionId)).toBeInTheDocument();
    expect(localStorage.getItem("hfu.voiceRegistration.sessionId")).toBe(sessionId);
  });

  it("connects live updates and joins the session group after creating a session", async () => {
    vi.stubGlobal(
      "fetch",
      createFetchMock({
        "POST /api/conversation-sessions": sessionResponse()
      })
    );

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "Создать сессию" }));

    expect(await screen.findByText(sessionId)).toBeInTheDocument();
    expect(realtimeClientMock.client.connect).toHaveBeenCalled();
    expect(realtimeClientMock.client.joinSession).toHaveBeenCalledWith(sessionId);
    expect(await screen.findByText("live подключено")).toBeInTheDocument();
  });

  it("keeps voice controls disabled before session creation", async () => {
    vi.stubGlobal("fetch", createFetchMock());

    render(<App />);

    expect(await screen.findByRole("button", { name: "Начать голос" })).toBeDisabled();
  });

  it("starts voice for the current session", async () => {
    vi.stubGlobal(
      "fetch",
      createFetchMock({
        "POST /api/conversation-sessions": sessionResponse()
      })
    );

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "Создать сессию" }));
    await screen.findByText(sessionId);
    fireEvent.click(screen.getByRole("button", { name: "Начать голос" }));

    expect(openAIRealtimeClientMock.createOpenAIRealtimeWebRtcClient).toHaveBeenCalledWith(
      expect.objectContaining({
        baseUrl: "",
        sessionId
      })
    );
    expect(openAIRealtimeClientMock.client.start).toHaveBeenCalled();
    await waitFor(() => {
      expect(openAIRealtimeClientMock.client.sendEvent).toHaveBeenCalledWith({
        type: "response.create",
        response: {
          instructions: expect.stringContaining("Start the HFU demo registration interview now")
        }
      });
    });
    expect(await screen.findByText("голос подключён")).toBeInTheDocument();
  });

  it("stops the active voice client", async () => {
    vi.stubGlobal(
      "fetch",
      createFetchMock({
        "POST /api/conversation-sessions": sessionResponse()
      })
    );

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "Создать сессию" }));
    await screen.findByText(sessionId);
    fireEvent.click(screen.getByRole("button", { name: "Начать голос" }));
    await screen.findByText("голос подключён");
    fireEvent.click(screen.getByRole("button", { name: "Остановить" }));

    expect(openAIRealtimeClientMock.client.stop).toHaveBeenCalled();
    expect(await screen.findByText("голос остановлен")).toBeInTheDocument();
  });

  it("renders voice transcript entries", async () => {
    vi.stubGlobal(
      "fetch",
      createFetchMock({
        "POST /api/conversation-sessions": sessionResponse()
      })
    );

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "Создать сессию" }));
    await screen.findByText(sessionId);
    fireEvent.click(screen.getByRole("button", { name: "Начать голос" }));
    openAIRealtimeClientMock.emitTranscript({
      id: "voice-1",
      role: "user",
      text: "Mene zvaty Dima",
      isFinal: true,
      occurredAt: "2026-07-22T12:00:00Z"
    });

    expect(await screen.findByText("Mene zvaty Dima")).toBeInTheDocument();
  });

  it("runs AI realtime tool calls through backend registration tools", async () => {
    const fetchMock = createFetchMock({
      "POST /api/conversation-sessions": sessionResponse(),
      [`POST /api/conversation-sessions/${sessionId}/tools/update-registration-fields`]: toolResult({
        state: stateWithFields([
          field("firstName", "AI Saved", "Captured")
        ])
      })
    });
    vi.stubGlobal("fetch", fetchMock);

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "Создать сессию" }));
    await screen.findByText(sessionId);
    fireEvent.click(screen.getByRole("button", { name: "Начать голос" }));
    await screen.findByText("голос подключён");

    openAIRealtimeClientMock.emitToolCall({
      id: "evt-tool",
      callId: "call-update",
      name: "update_registration_fields",
      argumentsJson: JSON.stringify({
        fields: [{ name: "firstName", value: "AI Saved" }]
      }),
      receivedAt: "2026-07-22T12:03:00Z"
    });

    expect(await screen.findByText("AI Saved")).toBeInTheDocument();
    expect(await screen.findByText("AI tools")).toBeInTheDocument();
    expect(await screen.findByText("update_registration_fields")).toBeInTheDocument();
  });

  it("restores a saved session on page load", async () => {
    vi.spyOn(Storage.prototype, "getItem").mockImplementation((key) =>
      key === "hfu.voiceRegistration.sessionId" ? sessionId : null
    );
    vi.stubGlobal(
      "fetch",
      createFetchMock({
        [`GET /api/conversation-sessions/${sessionId}`]: sessionResponse({
          status: "Active",
          state: stateWithFields([
            field("firstName", "Dimas", "Confirmed")
          ])
        })
      })
    );

    render(<App />);

    expect(await screen.findByText("Сессия восстановлена")).toBeInTheDocument();
    expect(await screen.findByText("Dimas")).toBeInTheDocument();
  });

  it("rejoins the live session group when restoring a saved session", async () => {
    vi.spyOn(Storage.prototype, "getItem").mockImplementation((key) =>
      key === "hfu.voiceRegistration.sessionId" ? sessionId : null
    );
    vi.stubGlobal(
      "fetch",
      createFetchMock({
        [`GET /api/conversation-sessions/${sessionId}`]: sessionResponse({
          status: "Active"
        })
      })
    );

    render(<App />);

    expect(await screen.findByText("Сессия восстановлена")).toBeInTheDocument();
    expect(realtimeClientMock.client.joinSession).toHaveBeenCalledWith(sessionId);
  });

  it("refreshes session state through HTTP after receiving a live event", async () => {
    const fetchMock = createFetchMock({
      "POST /api/conversation-sessions": sessionResponse(),
      [`GET /api/conversation-sessions/${sessionId}`]: sessionResponse({
        status: "Active",
        version: 3,
        state: stateWithFields([
          field("firstName", "Live", "Captured")
        ])
      })
    });
    vi.stubGlobal("fetch", fetchMock);

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "Создать сессию" }));
    await screen.findByText(sessionId);
    realtimeClientMock.emitEvent(liveEvent({
      type: "RegistrationStateChanged",
      message: "Registration state changed."
    }));

    expect(await screen.findByText("Registration state changed.")).toBeInTheDocument();
    expect(await screen.findByText("Live")).toBeInTheDocument();
  });

  it("renders Ukrainian region reference values", async () => {
    vi.stubGlobal("fetch", createFetchMock());

    render(<App />);

    expect((await screen.findAllByText("Харківська область")).length).toBeGreaterThan(0);
  });

  it("runs update, confirm, and complete tool actions from the UI", async () => {
    vi.stubGlobal(
      "fetch",
      createFetchMock({
        "POST /api/conversation-sessions": sessionResponse(),
        [`POST /api/conversation-sessions/${sessionId}/tools/update-registration-fields`]: toolResult({
          state: stateWithFields([
            field("firstName", "Dimas", "Captured"),
            field("currentRegion", "Харківська область", "Captured", "hfu-region-kharkivska")
          ])
        }),
        [`POST /api/conversation-sessions/${sessionId}/tools/confirm-registration-fields`]: toolResult({
          state: stateWithFields([
            field("firstName", "Dimas", "Confirmed"),
            field("currentRegion", "Харківська область", "Confirmed", "hfu-region-kharkivska")
          ])
        }),
        [`POST /api/conversation-sessions/${sessionId}/tools/complete-registration`]: toolResult({
          state: stateWithFields([], true),
          completion: completion()
        })
      })
    );

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "Создать сессию" }));
    await screen.findByText(sessionId);
    await waitFor(() => expect(screen.getByRole("button", { name: "Сохранить поля" })).not.toBeDisabled());

    fireEvent.click(screen.getByRole("button", { name: "Демо-данные" }));
    fireEvent.click(screen.getByRole("button", { name: "Сохранить поля" }));
    await waitFor(() => expect(screen.getByRole("button", { name: "Подтвердить поля" })).not.toBeDisabled());

    fireEvent.click(screen.getByRole("button", { name: "Подтвердить поля" }));
    await waitFor(() => expect(screen.getByRole("button", { name: "Завершить регистрацию" })).not.toBeDisabled());

    fireEvent.click(screen.getByLabelText("Согласие на обработку перс. данных"));
    fireEvent.click(screen.getByLabelText("Окончательное подтверждение регистрации"));
    fireEvent.click(screen.getByRole("button", { name: "Завершить регистрацию" }));

    expect(await screen.findByText("DEMO-2026-000001")).toBeInTheDocument();
  });

  it("displays structured business tool errors", async () => {
    vi.stubGlobal(
      "fetch",
      createFetchMock({
        "POST /api/conversation-sessions": sessionResponse(),
        [`POST /api/conversation-sessions/${sessionId}/tools/complete-registration`]: toolResult({
          succeeded: false,
          errors: [
            {
              code: "RegistrationCannotBeCompleted",
              field: null,
              message: "Registration cannot be completed until all validation issues are resolved."
            }
          ]
        })
      })
    );

    render(<App />);

    fireEvent.click(await screen.findByRole("button", { name: "Создать сессию" }));
    await screen.findByText(sessionId);
    await waitFor(() => expect(screen.getByRole("button", { name: "Завершить регистрацию" })).not.toBeDisabled());

    fireEvent.click(screen.getByRole("button", { name: "Завершить регистрацию" }));

    expect(await screen.findByText("RegistrationCannotBeCompleted")).toBeInTheDocument();
    expect(screen.getByText(/cannot be completed/i)).toBeInTheDocument();
  });
});

function createFetchMock(overrides: Record<string, unknown> = {}) {
  return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const path = url.startsWith("http")
      ? new URL(url).pathname
      : url;
    const method = init?.method ?? "GET";
    const key = `${method} ${path}`;
    const body = overrides[key] ?? defaultResponseFor(key);

    return Promise.resolve(jsonResponse(body));
  });
}

function defaultResponseFor(key: string): unknown {
  switch (key) {
    case "GET /health":
      return {
        status: "healthy",
        service: "Hfu.VoiceRegistration.Api",
        timestampUtc: "2026-07-22T12:00:00Z",
        version: "1.0.0.0"
      };
    case "GET /api/reference-data/regions":
      return {
        regions: [
          {
            id: "hfu-region-kharkivska",
            name: "Харківська область",
            aliases: ["Харківська", "Харьковская область"]
          },
          {
            id: "hfu-region-kyivska",
            name: "Київська область",
            aliases: ["Київська", "Киевская область"]
          }
        ]
      };
    default:
      return {};
  }
}

function jsonResponse(body: unknown) {
  return new Response(JSON.stringify(body), {
    status: 200,
    headers: { "Content-Type": "application/json" }
  });
}

function sessionResponse(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    sessionId,
    status: "Created",
    version: 0,
    createdAt: "2026-07-22T12:00:00Z",
    lastActivityAt: "2026-07-22T12:00:00Z",
    realtimeConnectionId: null,
    registrationResult: null,
    state: emptyState(),
    events: [],
    ...overrides
  };
}

function toolResult(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    succeeded: true,
    state: emptyState(),
    errors: [],
    completion: null,
    recommendedNextAction: null,
    ...overrides
  };
}

function emptyState(registrationCanBeCompleted = false) {
  return stateWithFields([], registrationCanBeCompleted);
}

function stateWithFields(fields: unknown[], registrationCanBeCompleted = false) {
  return {
    sessionId,
    version: 1,
    fields,
    missingRequiredFields: registrationCanBeCompleted ? [] : ["firstName", "lastName"],
    fieldsRequiringClarification: [],
    fieldsAwaitingConfirmation: [],
    registrationCanBeCompleted,
    completionIssues: []
  };
}

function field(
  name: string,
  value: unknown,
  status: string,
  referenceId: string | null = null
) {
  return {
    name,
    value,
    rawValue: String(value),
    status,
    clarificationReason: null,
    referenceId
  };
}

function completion() {
  return {
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
  };
}

function liveEvent(overrides: Partial<ConversationRealtimeEvent> = {}): ConversationRealtimeEvent {
  return {
    eventId: "22222222-2222-2222-2222-222222222222",
    sessionId,
    version: 2,
    type: "RegistrationStateChanged",
    message: "Registration state changed.",
    occurredAtUtc: "2026-07-22T12:01:00Z",
    correlationId: null,
    ...overrides
  };
}
