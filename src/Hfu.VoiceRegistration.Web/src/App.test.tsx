import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import App from "./App";

const sessionId = "11111111-1111-1111-1111-111111111111";

describe("App", () => {
  afterEach(() => {
    cleanup();
    localStorage.clear();
    vi.unstubAllGlobals();
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

  it("restores a saved session on page load", async () => {
    localStorage.setItem("hfu.voiceRegistration.sessionId", sessionId);
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
    expect(screen.getByText("Dimas")).toBeInTheDocument();
  });

  it("renders Ukrainian region reference values", async () => {
    vi.stubGlobal("fetch", createFetchMock());

    render(<App />);

    expect(await screen.findByText("Харківська область")).toBeInTheDocument();
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
    fireEvent.click(await screen.findByRole("button", { name: "Демо-данные" }));
    fireEvent.click(screen.getByRole("button", { name: "Сохранить поля" }));
    fireEvent.click(await screen.findByRole("button", { name: "Подтвердить заполненные" }));
    fireEvent.click(screen.getByLabelText("Согласие на обработку данных"));
    fireEvent.click(screen.getByLabelText("Финальное подтверждение"));
    fireEvent.click(screen.getByRole("button", { name: "Завершить регистрацию" }));

    expect(await screen.findByText("DEMO-2026-000001")).toBeInTheDocument();
    expect(screen.getByText("Регистрация завершена")).toBeInTheDocument();
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
