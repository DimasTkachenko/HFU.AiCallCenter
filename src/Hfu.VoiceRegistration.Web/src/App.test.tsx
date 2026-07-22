import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import App from "./App";

describe("App", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("shows the healthy backend state returned by the API", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            status: "healthy",
            service: "Hfu.VoiceRegistration.Api",
            timestampUtc: "2026-07-22T12:00:00Z",
            version: "1.0.0.0"
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        )
      )
    );

    render(<App />);

    expect(screen.getByText("HFU Voice Registration Demo")).toBeInTheDocument();
    expect(await screen.findAllByText("healthy")).toHaveLength(2);
    expect(screen.getAllByText("Hfu.VoiceRegistration.Api")).toHaveLength(2);
  });
});
