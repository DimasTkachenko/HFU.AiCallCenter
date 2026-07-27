import { createOpenAIRealtimeWebRtcClient } from "./openAIRealtimeClient";
import { createGeminiLiveClient } from "./geminiLiveClient";
import type {
  OpenAIRealtimeEventLogEntry,
  OpenAIRealtimeToolCall,
  OpenAIRealtimeTranscriptEntry,
  OpenAIRealtimeVoiceConnectionState
} from "./openAIRealtimeTypes";

export type AIProvider = "openai" | "gemini";

export interface CreateVoiceAssistantClientOptions {
  sessionId: string;
  baseUrl?: string;
  provider?: AIProvider;
}

export interface IVoiceAssistantClient {
  start: () => Promise<void>;
  stop: () => void;
  sendEvent?: (event: unknown) => void;
  onStateChange: (handler: (state: OpenAIRealtimeVoiceConnectionState) => void) => () => void;
  onTranscript?: (handler: (entry: OpenAIRealtimeTranscriptEntry) => void) => () => void;
  onEvent?: (handler: (event: OpenAIRealtimeEventLogEntry) => void) => () => void;
  onToolCall?: (handler: (toolCall: OpenAIRealtimeToolCall) => void) => () => void;
}

export function getEffectiveProvider(): AIProvider {
  const envProvider = (import.meta.env.VITE_AI_PROVIDER ?? "").toLowerCase().trim();
  if (envProvider === "gemini") {
    return "gemini";
  }
  return "openai";
}

export function createVoiceAssistantClient(
  options: CreateVoiceAssistantClientOptions
): IVoiceAssistantClient {
  const provider = options.provider ?? getEffectiveProvider();

  if (provider === "gemini") {
    return createGeminiLiveClient({
      sessionId: options.sessionId,
      baseUrl: options.baseUrl
    });
  }

  return createOpenAIRealtimeWebRtcClient({
    sessionId: options.sessionId,
    baseUrl: options.baseUrl
  });
}
