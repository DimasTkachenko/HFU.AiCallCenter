import { beforeEach, describe, expect, it, vi } from "vitest";
import { createConversationRealtimeClient } from "./conversationRealtimeClient";
import type { ConversationRealtimeEvent } from "./realtimeTypes";

const signalRMock = vi.hoisted(() => {
  const handlers = new Map<string, (value: unknown) => void>();
  const connection: {
    state: string;
    start: ReturnType<typeof vi.fn>;
    stop: ReturnType<typeof vi.fn>;
    invoke: ReturnType<typeof vi.fn>;
    on: ReturnType<typeof vi.fn>;
    onreconnecting: ReturnType<typeof vi.fn>;
    onreconnected: ReturnType<typeof vi.fn>;
    onclose: ReturnType<typeof vi.fn>;
  } = {
    state: "Disconnected",
    start: vi.fn(async () => {
      connection.state = "Connected";
    }),
    stop: vi.fn(async () => {
      connection.state = "Disconnected";
    }),
    invoke: vi.fn(),
    on: vi.fn((eventName: string, handler: (value: unknown) => void) => {
      handlers.set(eventName, handler);
    }),
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
    onclose: vi.fn()
  };
  const builder = {
    withUrl: vi.fn(() => builder),
    withAutomaticReconnect: vi.fn(() => builder),
    configureLogging: vi.fn(() => builder),
    build: vi.fn(() => connection)
  };

  return {
    builder,
    connection,
    handlers,
    HubConnectionBuilder: vi.fn(() => builder)
  };
});

vi.mock("@microsoft/signalr", () => ({
  HubConnectionBuilder: signalRMock.HubConnectionBuilder,
  HubConnectionState: {
    Connected: "Connected",
    Disconnected: "Disconnected"
  },
  LogLevel: {
    Warning: 2
  }
}));

describe("createConversationRealtimeClient", () => {
  beforeEach(() => {
    signalRMock.handlers.clear();
    signalRMock.connection.state = "Disconnected";
    signalRMock.connection.start.mockClear();
    signalRMock.connection.stop.mockClear();
    signalRMock.connection.invoke.mockClear();
    signalRMock.connection.on.mockClear();
    signalRMock.connection.onreconnecting.mockClear();
    signalRMock.connection.onreconnected.mockClear();
    signalRMock.connection.onclose.mockClear();
    signalRMock.builder.withUrl.mockClear();
    signalRMock.builder.withAutomaticReconnect.mockClear();
    signalRMock.builder.configureLogging.mockClear();
    signalRMock.builder.build.mockClear();
    signalRMock.HubConnectionBuilder.mockClear();
  });

  it("connects to the default conversation hub URL", async () => {
    const client = createConversationRealtimeClient();

    await client.connect();

    expect(signalRMock.builder.withUrl).toHaveBeenCalledWith("/hubs/conversation");
    expect(signalRMock.builder.withAutomaticReconnect).toHaveBeenCalled();
    expect(signalRMock.connection.start).toHaveBeenCalled();
  });

  it("connects to an absolute conversation hub URL when base URL is provided", async () => {
    const client = createConversationRealtimeClient({
      baseUrl: "http://localhost:5076/"
    });

    await client.connect();

    expect(signalRMock.builder.withUrl).toHaveBeenCalledWith("http://localhost:5076/hubs/conversation");
  });

  it("joins and leaves a session group", async () => {
    const client = createConversationRealtimeClient();
    await client.connect();

    await client.joinSession("11111111-1111-1111-1111-111111111111");
    await client.leaveSession("11111111-1111-1111-1111-111111111111");

    expect(signalRMock.connection.invoke).toHaveBeenCalledWith(
      "JoinSession",
      "11111111-1111-1111-1111-111111111111"
    );
    expect(signalRMock.connection.invoke).toHaveBeenCalledWith(
      "LeaveSession",
      "11111111-1111-1111-1111-111111111111"
    );
  });

  it("forwards typed ConversationEvent payloads", async () => {
    const client = createConversationRealtimeClient();
    const onEvent = vi.fn();
    const conversationEvent: ConversationRealtimeEvent = {
      eventId: "22222222-2222-2222-2222-222222222222",
      sessionId: "11111111-1111-1111-1111-111111111111",
      version: 2,
      type: "RegistrationStateChanged",
      message: "Registration state changed.",
      occurredAtUtc: "2026-07-22T12:00:00Z",
      correlationId: null
    };

    client.onEvent(onEvent);
    await client.connect();
    signalRMock.handlers.get("ConversationEvent")?.(conversationEvent);

    expect(onEvent).toHaveBeenCalledWith(conversationEvent);
  });
});
