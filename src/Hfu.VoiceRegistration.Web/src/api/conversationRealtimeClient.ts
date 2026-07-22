import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection
} from "@microsoft/signalr";
import type {
  ConversationRealtimeEvent,
  RealtimeConnectionState
} from "./realtimeTypes";

export interface CreateConversationRealtimeClientOptions {
  baseUrl?: string;
}

export interface ConversationRealtimeClient {
  connect: () => Promise<void>;
  joinSession: (sessionId: string) => Promise<void>;
  leaveSession: (sessionId: string) => Promise<void>;
  onEvent: (handler: (conversationEvent: ConversationRealtimeEvent) => void) => () => void;
  onStatusChange: (handler: (state: RealtimeConnectionState) => void) => () => void;
  stop: () => Promise<void>;
}

export function createConversationRealtimeClient(
  options: CreateConversationRealtimeClientOptions = {}
): ConversationRealtimeClient {
  let connection: HubConnection | null = null;
  const eventHandlers = new Set<(conversationEvent: ConversationRealtimeEvent) => void>();
  const statusHandlers = new Set<(state: RealtimeConnectionState) => void>();
  const hubUrl = buildHubUrl(options.baseUrl);

  function emitStatus(state: RealtimeConnectionState) {
    for (const handler of statusHandlers) {
      handler(state);
    }
  }

  function ensureConnection() {
    if (connection) {
      return connection;
    }

    connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("ConversationEvent", (conversationEvent: ConversationRealtimeEvent) => {
      for (const handler of eventHandlers) {
        handler(conversationEvent);
      }
    });
    connection.onreconnecting((error) => {
      emitStatus({
        status: "reconnecting",
        message: errorMessage(error)
      });
    });
    connection.onreconnected(() => {
      emitStatus({ status: "connected" });
    });
    connection.onclose((error) => {
      emitStatus(error
        ? { status: "error", message: errorMessage(error) }
        : { status: "disconnected" });
    });

    return connection;
  }

  async function connect() {
    const activeConnection = ensureConnection();
    if (activeConnection.state === HubConnectionState.Connected) {
      return;
    }

    emitStatus({ status: "connecting" });
    try {
      await activeConnection.start();
      emitStatus({ status: "connected" });
    } catch (error) {
      emitStatus({ status: "error", message: errorMessage(error) });
      throw error;
    }
  }

  async function joinSession(sessionId: string) {
    await connect();
    await ensureConnection().invoke("JoinSession", sessionId);
  }

  async function leaveSession(sessionId: string) {
    const activeConnection = ensureConnection();
    if (activeConnection.state !== HubConnectionState.Connected) {
      return;
    }

    await activeConnection.invoke("LeaveSession", sessionId);
  }

  return {
    connect,
    joinSession,
    leaveSession,
    onEvent(handler) {
      eventHandlers.add(handler);

      return () => eventHandlers.delete(handler);
    },
    onStatusChange(handler) {
      statusHandlers.add(handler);

      return () => statusHandlers.delete(handler);
    },
    async stop() {
      if (!connection) {
        return;
      }

      await connection.stop();
      emitStatus({ status: "disconnected" });
    }
  };
}

function buildHubUrl(baseUrl: string | undefined) {
  const normalizedBaseUrl = baseUrl?.trim().replace(/\/+$/, "");

  return normalizedBaseUrl
    ? `${normalizedBaseUrl}/hubs/conversation`
    : "/hubs/conversation";
}

function errorMessage(error: unknown) {
  return error instanceof Error
    ? error.message
    : "Не удалось подключиться к live updates.";
}
