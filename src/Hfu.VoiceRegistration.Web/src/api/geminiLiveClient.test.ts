import { beforeEach, describe, expect, it, vi } from "vitest";
import { createGeminiLiveClient } from "./geminiLiveClient";

describe("createGeminiLiveClient", () => {
  beforeEach(() => {
    vi.unstubAllGlobals();
    FakeWebSocket.instances = [];
    FakeAudioContext.instances = [];
  });

  it("waits for backend input-enabled marker before sending microphone audio", async () => {
    vi.stubGlobal("WebSocket", FakeWebSocket);
    vi.stubGlobal("AudioContext", FakeAudioContext);
    vi.stubGlobal("webkitAudioContext", FakeAudioContext);

    const client = createGeminiLiveClient({
      sessionId: "11111111-1111-1111-1111-111111111111",
      baseUrl: "http://localhost:5076",
      mediaDevices: fakeMediaDevices()
    });

    await client.start();
    const webSocket = FakeWebSocket.instances[0];
    webSocket.open();

    FakeAudioContext.instances[0].scriptProcessor!.process(new Float32Array(4096).fill(0.25));
    expect(webSocket.sentMessages).toEqual([]);

    webSocket.emitBinary(new Uint8Array([0x03]).buffer);

    FakeAudioContext.instances[0].scriptProcessor!.process(new Float32Array(4096).fill(0.25));
    expect(webSocket.sentMessages).toHaveLength(1);
    expect(webSocket.sentMessages[0]).toBeInstanceOf(ArrayBuffer);

    webSocket.emitBinary(new Uint8Array([0x04]).buffer);

    FakeAudioContext.instances[0].scriptProcessor!.process(new Float32Array(4096).fill(0.25));
    expect(webSocket.sentMessages).toHaveLength(1);
  });

  it("waits for queued assistant playback to finish after backend input-enabled marker", async () => {
    vi.stubGlobal("WebSocket", FakeWebSocket);
    vi.stubGlobal("AudioContext", FakeAudioContext);
    vi.stubGlobal("webkitAudioContext", FakeAudioContext);

    const client = createGeminiLiveClient({
      sessionId: "11111111-1111-1111-1111-111111111111",
      baseUrl: "http://localhost:5076",
      mediaDevices: fakeMediaDevices()
    });

    await client.start();
    const webSocket = FakeWebSocket.instances[0];
    webSocket.open();

    const playbackContext = FakeAudioContext.instances[1];
    webSocket.emitBinary(new Uint8Array([0x01, 1, 0, 2, 0]).buffer);
    webSocket.emitBinary(new Uint8Array([0x03]).buffer);

    FakeAudioContext.instances[0].scriptProcessor!.process(new Float32Array(4096).fill(0.25));
    expect(webSocket.sentMessages).toEqual([]);

    playbackContext.sources[0].finish();

    FakeAudioContext.instances[0].scriptProcessor!.process(new Float32Array(4096).fill(0.25));
    expect(webSocket.sentMessages).toHaveLength(1);
    expect(webSocket.sentMessages[0]).toBeInstanceOf(ArrayBuffer);
  });
});

function fakeMediaDevices() {
  const track = { stop: vi.fn() };
  const stream = {
    getTracks: vi.fn(() => [track])
  };

  return {
    getUserMedia: vi.fn(async () => stream as unknown as MediaStream)
  };
}

class FakeWebSocket {
  static readonly CONNECTING = 0;
  static readonly OPEN = 1;
  static readonly CLOSING = 2;
  static readonly CLOSED = 3;

  static instances: FakeWebSocket[] = [];

  readyState = FakeWebSocket.CONNECTING;
  binaryType: BinaryType = "blob";
  onopen: ((event: Event) => void) | null = null;
  onmessage: ((event: MessageEvent<ArrayBuffer>) => void) | null = null;
  onerror: ((event: Event) => void) | null = null;
  onclose: ((event: CloseEvent) => void) | null = null;
  sentMessages: unknown[] = [];

  constructor(public readonly url: string) {
    FakeWebSocket.instances.push(this);
  }

  open() {
    this.readyState = FakeWebSocket.OPEN;
    this.onopen?.(new Event("open"));
  }

  send(data: unknown) {
    this.sentMessages.push(data);
  }

  emitBinary(data: ArrayBuffer) {
    this.onmessage?.({ data } as MessageEvent<ArrayBuffer>);
  }

  close() {
    this.readyState = FakeWebSocket.CLOSED;
  }
}

class FakeAudioContext {
  static instances: FakeAudioContext[] = [];

  sampleRate: number;
  destination = {};
  scriptProcessor: FakeScriptProcessor | null = null;
  sources: FakeAudioBufferSource[] = [];

  constructor(options?: AudioContextOptions) {
    this.sampleRate = options?.sampleRate ?? 48000;
    FakeAudioContext.instances.push(this);
  }

  createMediaStreamSource() {
    return {
      connect: vi.fn(),
      disconnect: vi.fn()
    };
  }

  createScriptProcessor() {
    this.scriptProcessor = new FakeScriptProcessor();

    return this.scriptProcessor;
  }

  createBuffer(numberOfChannels: number, length: number, sampleRate: number) {
    return {
      duration: length / sampleRate,
      getChannelData: () => new Float32Array(length),
      numberOfChannels,
      sampleRate
    };
  }

  createBufferSource() {
    const source = new FakeAudioBufferSource();
    this.sources.push(source);

    return source;
  }

  close() {
    return Promise.resolve();
  }
}

class FakeAudioBufferSource {
  buffer: AudioBuffer | null = null;
  connect = vi.fn();
  start = vi.fn();
  stop = vi.fn();
  onended: (() => void) | null = null;

  finish() {
    this.onended?.();
  }
}

class FakeScriptProcessor {
  onaudioprocess: ((event: AudioProcessingEvent) => void) | null = null;

  connect = vi.fn();
  disconnect = vi.fn();

  process(samples: Float32Array) {
    this.onaudioprocess?.({
      inputBuffer: {
        getChannelData: () => samples
      }
    } as unknown as AudioProcessingEvent);
  }
}
