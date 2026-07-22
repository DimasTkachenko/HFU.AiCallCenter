export type OpenAIRealtimeVoiceConnectionStatus =
  | "idle"
  | "requesting_microphone"
  | "connecting"
  | "connected"
  | "stopped"
  | "error";

export interface OpenAIRealtimeVoiceConnectionState {
  status: OpenAIRealtimeVoiceConnectionStatus;
  message?: string;
}

export interface OpenAIRealtimeTranscriptEntry {
  id: string;
  role: "user" | "assistant";
  text: string;
  isFinal: boolean;
  occurredAt: string;
}

export interface OpenAIRealtimeEventLogEntry {
  id: string;
  type: string;
  receivedAt: string;
}

export interface CreateOpenAIRealtimeWebRtcClientOptions {
  sessionId: string;
  baseUrl?: string;
  mediaDevices?: Pick<MediaDevices, "getUserMedia">;
  peerConnectionFactory?: () => RTCPeerConnection;
  audioElementFactory?: () => HTMLAudioElement;
}

export interface OpenAIRealtimeWebRtcClient {
  start: () => Promise<void>;
  stop: () => void;
  sendEvent: (event: unknown) => void;
  onStateChange: (handler: (state: OpenAIRealtimeVoiceConnectionState) => void) => () => void;
  onTranscript: (handler: (entry: OpenAIRealtimeTranscriptEntry) => void) => () => void;
  onEvent: (handler: (event: OpenAIRealtimeEventLogEntry) => void) => () => void;
}
