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

export function setStoredProvider(provider: AIProvider): void {
  if (typeof window !== "undefined") {
    window.localStorage.setItem("hfu.voiceRegistration.provider", provider);
  }
}

export function getEffectiveProvider(): AIProvider {
  if (typeof window !== "undefined") {
    const urlParam = new URLSearchParams(window.location.search).get("provider")?.toLowerCase().trim();
    if (urlParam === "gemini" || urlParam === "openai") {
      return urlParam;
    }
    const stored = window.localStorage.getItem("hfu.voiceRegistration.provider")?.toLowerCase().trim();
    if (stored === "gemini" || stored === "openai") {
      return stored;
    }
  }

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
