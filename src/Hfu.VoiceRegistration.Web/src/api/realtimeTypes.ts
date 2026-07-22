export type ConversationRealtimeEventType =
  | "SessionCreated"
  | "SessionUpdated"
  | "RegistrationStateChanged"
  | "RegistrationToolCompleted"
  | "RegistrationCompleted"
  | "SessionAbandoned"
  | "DiagnosticEventAdded"
  | "TranscriptReceived"
  | "ToolCallReceived"
  | "ToolCallCompleted"
  | "ValidationFailed"
  | "ConnectionStatusChanged";

export interface ConversationRealtimeEvent {
  eventId: string;
  sessionId: string;
  version: number;
  type: ConversationRealtimeEventType;
  message: string;
  occurredAtUtc: string;
  correlationId?: string | null;
}

export type RealtimeConnectionStatus =
  | "idle"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected"
  | "error";

export interface RealtimeConnectionState {
  status: RealtimeConnectionStatus;
  message?: string;
}
