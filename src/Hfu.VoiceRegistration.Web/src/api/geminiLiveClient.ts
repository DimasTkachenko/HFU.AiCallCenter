import type {
  OpenAIRealtimeVoiceConnectionState,
  OpenAIRealtimeVoiceConnectionStatus
} from "./openAIRealtimeTypes";

export interface CreateGeminiLiveClientOptions {
  sessionId: string;
  baseUrl?: string;
  mediaDevices?: Pick<MediaDevices, "getUserMedia">;
}

export interface GeminiLiveWebClient {
  start: () => Promise<void>;
  stop: () => void;
  onStateChange: (handler: (state: OpenAIRealtimeVoiceConnectionState) => void) => () => void;
  onTranscript?: (handler: (entry: any) => void) => () => void;
  onEvent?: (handler: (event: any) => void) => () => void;
  onToolCall?: (handler: (toolCall: any) => void) => () => void;
}

export function createGeminiLiveClient(
  options: CreateGeminiLiveClientOptions
): GeminiLiveWebClient {
  const stateHandlers = new Set<(state: OpenAIRealtimeVoiceConnectionState) => void>();

  let webSocket: WebSocket | null = null;
  let audioContext: AudioContext | null = null;
  let micStream: MediaStream | null = null;
  let micSource: MediaStreamAudioSourceNode | null = null;
  let scriptProcessor: ScriptProcessorNode | null = null;
  let hasBackendEnabledInputAudio = false;
  let isInputAudioEnabled = false;

  // Audio Playback Queue
  let playbackAudioContext: AudioContext | null = null;
  let nextStartTime = 0;
  let activeSources: AudioBufferSourceNode[] = [];

  function emitState(state: OpenAIRealtimeVoiceConnectionState) {
    for (const handler of stateHandlers) {
      handler(state);
    }
  }

  function getWsUrl(): string {
    const base = options.baseUrl || window.location.origin;
    const wsBase = base.replace(/^http/, "ws");
    return `${wsBase}/api/conversation-sessions/${options.sessionId}/gemini-live/stream`;
  }

  async function start() {
    if (webSocket) return;

    try {
      emitState({ status: "requesting_microphone" });
      const mediaDevices = options.mediaDevices ?? navigator.mediaDevices;
      if (!mediaDevices?.getUserMedia) {
        throw new Error("Microphone capture is not available in this browser.");
      }

      micStream = await mediaDevices.getUserMedia({ audio: true });
      emitState({ status: "connecting" });

      const wsUrl = getWsUrl();
      webSocket = new WebSocket(wsUrl);
      webSocket.binaryType = "arraybuffer";

      let isErrored = false;

      webSocket.onopen = () => {
        emitState({ status: "connected" });
        setupMicrophoneProcessing(micStream!);
        setupAudioPlayback();
      };

      webSocket.onmessage = (event: MessageEvent) => {
        if (event.data instanceof ArrayBuffer) {
          handleIncomingBinaryData(event.data);
        }
      };

      webSocket.onerror = (errEvent) => {
        isErrored = true;
        console.error("Gemini Live WebSocket error:", errEvent);
        emitState({ status: "error", message: "Gemini Live WebSocket connection failed." });
        cleanup();
      };

      webSocket.onclose = (closeEvent) => {
        if (!isErrored) {
          if (closeEvent.reason) {
            emitState({ status: "error", message: `Connection closed: ${closeEvent.reason}` });
          } else if (closeEvent.code !== 1000) {
            emitState({ status: "error", message: `Connection closed with code ${closeEvent.code}` });
          } else {
            emitState({ status: "stopped" });
          }
        }
        cleanup();
      };
    } catch (err: any) {
      emitState({ status: "error", message: err?.message || "Failed to start Gemini Live connection." });
      cleanup();
    }
  }

  function setupMicrophoneProcessing(stream: MediaStream) {
    audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
    micSource = audioContext.createMediaStreamSource(stream);
    scriptProcessor = audioContext.createScriptProcessor(4096, 1, 1);

    const inputSampleRate = audioContext.sampleRate;
    const targetSampleRate = 16000;

    scriptProcessor.onaudioprocess = (e: AudioProcessingEvent) => {
      if (!webSocket || webSocket.readyState !== WebSocket.OPEN) return;
      if (!isInputAudioEnabled) return;

      const inputData = e.inputBuffer.getChannelData(0);
      const resampledData = resampleTo16kMono(inputData, inputSampleRate, targetSampleRate);
      const pcmInt16 = convertFloat32ToInt16(resampledData);

      webSocket.send(pcmInt16.buffer);
    };

    micSource.connect(scriptProcessor);
    scriptProcessor.connect(audioContext.destination);
  }

  function resampleTo16kMono(
    inputData: Float32Array,
    fromSampleRate: number,
    toSampleRate: number
  ): Float32Array {
    if (fromSampleRate === toSampleRate) {
      return inputData;
    }
    const ratio = fromSampleRate / toSampleRate;
    const newLength = Math.round(inputData.length / ratio);
    const result = new Float32Array(newLength);
    for (let i = 0; i < newLength; i++) {
      const originIndex = Math.round(i * ratio);
      result[i] = inputData[originIndex] || 0;
    }
    return result;
  }

  function convertFloat32ToInt16(buffer: Float32Array): Int16Array {
    const l = buffer.length;
    const buf = new Int16Array(l);
    for (let i = 0; i < l; i++) {
      const s = Math.max(-1, Math.min(1, buffer[i]));
      buf[i] = s < 0 ? s * 0x8000 : s * 0x7fff;
    }
    return buf;
  }

  function setupAudioPlayback() {
    playbackAudioContext = new (window.AudioContext || (window as any).webkitAudioContext)({ sampleRate: 24000 });
    nextStartTime = playbackAudioContext.currentTime;
    activeSources = [];
  }

  function handleIncomingBinaryData(arrayBuffer: ArrayBuffer) {
    if (arrayBuffer.byteLength === 0) return;
    const view = new Uint8Array(arrayBuffer);
    const marker = view[0];

    if (marker === 0x02) {
      // Interrupted: stop current audio playback
      flushPlaybackQueue();
      return;
    }

    if (marker === 0x03) {
      // Backend has confirmed the assistant opening turn finished.
      hasBackendEnabledInputAudio = true;
      enableInputAudioIfPlaybackIdle();
      return;
    }

    if (marker === 0x04) {
      // Backend is processing a model/tool turn, so user audio must pause.
      hasBackendEnabledInputAudio = false;
      isInputAudioEnabled = false;
      return;
    }

    if (marker === 0x01 && arrayBuffer.byteLength > 1) {
      // Audio chunk: PCM 24kHz 16-bit Mono
      const pcmBytes = arrayBuffer.slice(1);
      playPcm24kChunk(pcmBytes);
    }
  }

  function playPcm24kChunk(pcmBuffer: ArrayBuffer) {
    if (!playbackAudioContext) return;
    isInputAudioEnabled = false;

    const int16Array = new Int16Array(pcmBuffer);
    const float32Array = new Float32Array(int16Array.length);

    for (let i = 0; i < int16Array.length; i++) {
      float32Array[i] = int16Array[i] / (int16Array[i] < 0 ? 0x8000 : 0x7fff);
    }

    const audioBuffer = playbackAudioContext.createBuffer(1, float32Array.length, 24000);
    audioBuffer.getChannelData(0).set(float32Array);

    const source = playbackAudioContext.createBufferSource();
    source.buffer = audioBuffer;
    source.connect(playbackAudioContext.destination);

    const currentTime = playbackAudioContext.currentTime;
    const startTime = Math.max(currentTime, nextStartTime);
    source.start(startTime);
    nextStartTime = startTime + audioBuffer.duration;

    activeSources.push(source);
    source.onended = () => {
      const idx = activeSources.indexOf(source);
      if (idx !== -1) activeSources.splice(idx, 1);
      enableInputAudioIfPlaybackIdle();
    };
  }

  function enableInputAudioIfPlaybackIdle() {
    if (!hasBackendEnabledInputAudio || activeSources.length > 0) {
      return;
    }

    isInputAudioEnabled = true;
  }

  function flushPlaybackQueue() {
    for (const src of activeSources) {
      try {
        src.stop();
      } catch {}
    }
    activeSources = [];
    if (playbackAudioContext) {
      nextStartTime = playbackAudioContext.currentTime;
    }
  }

  function cleanup() {
    webSocket = null;
    hasBackendEnabledInputAudio = false;
    isInputAudioEnabled = false;
    flushPlaybackQueue();

    if (scriptProcessor) {
      scriptProcessor.disconnect();
      scriptProcessor = null;
    }
    if (micSource) {
      micSource.disconnect();
      micSource = null;
    }
    if (audioContext) {
      audioContext.close();
      audioContext = null;
    }
    if (playbackAudioContext) {
      playbackAudioContext.close();
      playbackAudioContext = null;
    }
    if (micStream) {
      micStream.getTracks().forEach((t) => t.stop());
      micStream = null;
    }
  }

  function stop() {
    if (webSocket && webSocket.readyState === WebSocket.OPEN) {
      webSocket.close();
    }
    cleanup();
    emitState({ status: "stopped" });
  }

  function onStateChange(handler: (state: OpenAIRealtimeVoiceConnectionState) => void) {
    stateHandlers.add(handler);
    return () => {
      stateHandlers.delete(handler);
    };
  }

  function onTranscript() {
    return () => {};
  }

  function onEvent() {
    return () => {};
  }

  function onToolCall() {
    return () => {};
  }

  return {
    start,
    stop,
    onStateChange,
    onTranscript,
    onEvent,
    onToolCall
  };
}
