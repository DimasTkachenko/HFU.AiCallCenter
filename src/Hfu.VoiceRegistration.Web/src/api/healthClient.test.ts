import { describe, expect, it, vi } from "vitest";
import { fetchHealth } from "./healthClient";

describe("fetchHealth", () => {
  it("returns typed health payload from the configured backend", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          status: "healthy",
          service: "Hfu.VoiceRegistration.Api",
          timestampUtc: "2026-07-22T12:00:00Z",
          version: "1.0.0.0"
        }),
        { status: 200, headers: { "Content-Type": "application/json" } }
      )
    );

    vi.stubGlobal("fetch", fetchMock);

    const health = await fetchHealth("http://localhost:5080");

    expect(fetchMock).toHaveBeenCalledWith("http://localhost:5080/health", {
      headers: { Accept: "application/json" }
    });
    expect(health.status).toBe("healthy");
    expect(health.service).toBe("Hfu.VoiceRegistration.Api");
  });

  it("throws a readable error when the backend is unavailable", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("", { status: 503 })));

    await expect(fetchHealth()).rejects.toThrow("Backend health check failed with status 503.");
  });
});
