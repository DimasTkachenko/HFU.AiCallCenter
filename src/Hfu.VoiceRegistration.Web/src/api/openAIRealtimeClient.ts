import type { ProblemDetails } from "./registrationTypes";
import type {
  CreateOpenAIRealtimeWebRtcClientOptions,
  OpenAIRealtimeEventLogEntry,
  OpenAIRealtimeTranscriptEntry,
  OpenAIRealtimeVoiceConnectionState,
  OpenAIRealtimeWebRtcClient
} from "./openAIRealtimeTypes";

const sdpHeaders = {
  Accept: "application/sdp",
  "Content-Type": "application/sdp"
};

export async function startOpenAIRealtimeCall(
  sessionId: string,
  sdpOffer: string,
  baseUrl = "",
  signal?: AbortSignal
): Promise<string> {
  const response = await fetch(
    toUrl(`/api/conversation-sessions/${sessionId}/realtime/calls`, baseUrl),
    {
      method: "POST",
      headers: sdpHeaders,
      body: sdpOffer,
      signal
    }
  );

  if (response.ok) {
    return response.text();
  }

  throw await parseProblemDetails(response);
}

export function createOpenAIRealtimeWebRtcClient(
  options: CreateOpenAIRealtimeWebRtcClientOptions
): OpenAIRealtimeWebRtcClient {
  const stateHandlers = new Set<(state: OpenAIRealtimeVoiceConnectionState) => void>();
  const transcriptHandlers = new Set<(entry: OpenAIRealtimeTranscriptEntry) => void>();
  const eventHandlers = new Set<(event: OpenAIRealtimeEventLogEntry) => void>();

  let peerConnection: RTCPeerConnection | null = null;
  let dataChannel: RTCDataChannel | null = null;
  let localStream: MediaStream | null = null;
  let assistantDraft: OpenAIRealtimeTranscriptEntry | null = null;
  let abortController: AbortController | null = null;
  let isStopped = false;
  let startVersion = 0;

  function emitState(state: OpenAIRealtimeVoiceConnectionState) {
    for (const handler of stateHandlers) {
      handler(state);
    }
  }

  function emitTranscript(entry: OpenAIRealtimeTranscriptEntry) {
    for (const handler of transcriptHandlers) {
      handler(entry);
    }
  }

  function emitEvent(event: OpenAIRealtimeEventLogEntry) {
    for (const handler of eventHandlers) {
      handler(event);
    }
  }

  async function start() {
    if (peerConnection) {
      return;
    }

    isStopped = false;
    const currentStartVersion = ++startVersion;

    try {
      emitState({ status: "requesting_microphone" });
      const mediaDevices = options.mediaDevices ?? navigator.mediaDevices;
      if (!mediaDevices?.getUserMedia) {
        throw new Error("Microphone capture is not available in this browser.");
      }

      localStream = await mediaDevices.getUserMedia({ audio: true });
      if (!isCurrentStart(currentStartVersion)) {
        cleanup();
        return;
      }

      emitState({ status: "connecting" });
      peerConnection = options.peerConnectionFactory?.() ?? new RTCPeerConnection();
      const remoteAudio = options.audioElementFactory?.() ?? new Audio();
      remoteAudio.autoplay = true;
      peerConnection.ontrack = (event) => {
        remoteAudio.srcObject = event.streams[0] ?? null;
      };

      for (const track of localStream.getAudioTracks()) {
        peerConnection.addTrack(track, localStream);
      }
      if (!isCurrentStart(currentStartVersion)) {
        cleanup();
        return;
      }

      dataChannel = peerConnection.createDataChannel("oai-events");
      dataChannel.onmessage = handleDataChannelMessage;

      const offer = await peerConnection.createOffer();
      if (!isCurrentStart(currentStartVersion)) {
        cleanup();
        return;
      }

      if (!offer.sdp) {
        throw new Error("WebRTC offer did not contain SDP.");
      }

      await peerConnection.setLocalDescription(offer);
      if (!isCurrentStart(currentStartVersion)) {
        cleanup();
        return;
      }

      abortController = new AbortController();
      const answerSdp = await startOpenAIRealtimeCall(
        options.sessionId,
        offer.sdp,
        options.baseUrl,
        abortController.signal
      );
      abortController = null;
      if (!isCurrentStart(currentStartVersion)) {
        cleanup();
        return;
      }

      await peerConnection.setRemoteDescription({
        type: "answer",
        sdp: answerSdp
      });
      if (!isCurrentStart(currentStartVersion)) {
        cleanup();
        return;
      }

      emitState({ status: "connected" });
    } catch (error) {
      if (!isCurrentStart(currentStartVersion)) {
        cleanup();
        return;
      }

      cleanup();
      emitState({
        status: "error",
        message: errorMessage(error)
      });
      throw error;
    }
  }

  function stop() {
    isStopped = true;
    startVersion += 1;
    cleanup();
    emitState({ status: "stopped" });
  }

  function sendEvent(event: unknown) {
    if (!dataChannel || dataChannel.readyState === "closed") {
      throw new Error("Realtime data channel is not connected.");
    }

    dataChannel.send(JSON.stringify(event));
  }

  function cleanup() {
    try {
      abortController?.abort();
    } catch {
      // AbortController implementations can throw if the signal is already aborted.
    }

    try {
      dataChannel?.close();
    } catch {
      // Browser implementations can throw when a channel is already closing.
    }

    try {
      peerConnection?.close();
    } catch {
      // Browser implementations can throw when a peer connection is already closed.
    }

    for (const track of localStream?.getTracks() ?? []) {
      track.stop();
    }

    dataChannel = null;
    peerConnection = null;
    localStream = null;
    assistantDraft = null;
    abortController = null;
  }

  function isCurrentStart(currentStartVersion: number) {
    return !isStopped && currentStartVersion === startVersion;
  }

  function handleDataChannelMessage(event: MessageEvent) {
    const payload = parseRealtimePayload(event.data);
    const type = stringField(payload, "type") ?? "unknown";
    const eventId = stringField(payload, "event_id")
      ?? stringField(payload, "eventId")
      ?? createClientEventId();
    const receivedAt = new Date().toISOString();

    emitEvent({
      id: eventId,
      type,
      receivedAt
    });

    if (type === "conversation.item.input_audio_transcription.completed") {
      const transcript = transcriptText(payload);
      if (!transcript) {
        return;
      }

      emitTranscript({
        id: eventId,
        role: "user",
        text: transcript,
        isFinal: true,
        occurredAt: receivedAt
      });
      return;
    }

    if (type === "response.audio_transcript.delta") {
      const delta = stringField(payload, "delta");
      if (!delta) {
        return;
      }

      assistantDraft = {
        id: assistantDraft?.isFinal === false ? assistantDraft.id : eventId,
        role: "assistant",
        text: `${assistantDraft?.isFinal === false ? assistantDraft.text : ""}${delta}`,
        isFinal: false,
        occurredAt: assistantDraft?.isFinal === false ? assistantDraft.occurredAt : receivedAt
      };
      emitTranscript(assistantDraft);
      return;
    }

    if (type === "response.audio_transcript.done") {
      const transcript = stringField(payload, "transcript") ?? assistantDraft?.text ?? "";
      if (!transcript) {
        return;
      }

      assistantDraft = {
        id: assistantDraft?.id ?? eventId,
        role: "assistant",
        text: transcript,
        isFinal: true,
        occurredAt: assistantDraft?.occurredAt ?? receivedAt
      };
      emitTranscript(assistantDraft);
    }
  }

  return {
    start,
    stop,
    sendEvent,
    onStateChange(handler) {
      stateHandlers.add(handler);

      return () => stateHandlers.delete(handler);
    },
    onTranscript(handler) {
      transcriptHandlers.add(handler);

      return () => transcriptHandlers.delete(handler);
    },
    onEvent(handler) {
      eventHandlers.add(handler);

      return () => eventHandlers.delete(handler);
    }
  };
}

async function parseProblemDetails(response: Response): Promise<ProblemDetails> {
  try {
    const parsed = await response.json() as Partial<ProblemDetails>;

    return {
      title: parsed.title ?? "Realtime request failed",
      status: parsed.status ?? response.status,
      detail: parsed.detail
    };
  } catch {
    return {
      title: "Realtime request failed",
      status: response.status,
      detail: `Request failed with status ${response.status}.`
    };
  }
}

function toUrl(path: string, baseUrl: string): string {
  return `${baseUrl.replace(/\/$/, "")}${path}`;
}

function parseRealtimePayload(data: unknown): Record<string, unknown> {
  if (typeof data !== "string") {
    return {};
  }

  try {
    const parsed = JSON.parse(data);
    return isRecord(parsed) ? parsed : {};
  } catch {
    return {};
  }
}

function transcriptText(payload: Record<string, unknown>): string | null {
  const transcript = stringField(payload, "transcript");
  if (transcript) {
    return transcript;
  }

  const item = recordField(payload, "item");
  const content = Array.isArray(item?.content) ? item.content : [];
  const transcriptContent = content.find((entry): entry is Record<string, unknown> =>
    isRecord(entry) && typeof entry.transcript === "string");

  return transcriptContent
    ? transcriptContent.transcript as string
    : null;
}

function stringField(payload: Record<string, unknown>, fieldName: string): string | null {
  const value = payload[fieldName];

  return typeof value === "string" && value.length > 0
    ? value
    : null;
}

function recordField(payload: Record<string, unknown>, fieldName: string): Record<string, unknown> | null {
  const value = payload[fieldName];

  return isRecord(value) ? value : null;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function createClientEventId(): string {
  return crypto.randomUUID?.() ?? `client-${Date.now()}`;
}

function errorMessage(error: unknown) {
  if (isProblemDetails(error)) {
    return error.detail ?? error.title;
  }

  return error instanceof Error
    ? error.message
    : "Realtime voice connection failed.";
}

function isProblemDetails(error: unknown): error is ProblemDetails {
  return isRecord(error)
    && typeof error.title === "string"
    && typeof error.status === "number";
}
