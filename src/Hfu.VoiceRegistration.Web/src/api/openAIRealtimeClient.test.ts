import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  createOpenAIRealtimeWebRtcClient,
  startOpenAIRealtimeCall
} from "./openAIRealtimeClient";
import type { OpenAIRealtimeTranscriptEntry } from "./openAIRealtimeTypes";

describe("startOpenAIRealtimeCall", () => {
  beforeEach(() => {
    vi.unstubAllGlobals();
  });

  it("posts raw SDP to the conversation realtime calls endpoint", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response("answer-sdp", {
        status: 200,
        headers: { "Content-Type": "application/sdp" }
      })
    );
    vi.stubGlobal("fetch", fetchMock);

    const answer = await startOpenAIRealtimeCall(
      "11111111-1111-1111-1111-111111111111",
      "offer-sdp",
      "http://localhost:5076/"
    );

    expect(answer).toBe("answer-sdp");
    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5076/api/conversation-sessions/11111111-1111-1111-1111-111111111111/realtime/calls",
      {
        method: "POST",
        headers: {
          Accept: "application/sdp",
          "Content-Type": "application/sdp"
        },
        body: "offer-sdp"
      }
    );
  });
});

describe("createOpenAIRealtimeWebRtcClient", () => {
  beforeEach(() => {
    vi.unstubAllGlobals();
  });

  it("starts a WebRTC call through the backend SDP endpoint", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response("answer-sdp", { status: 200 }))
    );
    const media = fakeMediaDevices();
    const peerConnection = new FakePeerConnection();
    const states: string[] = [];
    const client = createOpenAIRealtimeWebRtcClient({
      sessionId: "22222222-2222-2222-2222-222222222222",
      mediaDevices: media,
      peerConnectionFactory: () => peerConnection as unknown as RTCPeerConnection,
      audioElementFactory: () => fakeAudioElement()
    });
    client.onStateChange((state) => states.push(state.status));

    await client.start();

    expect(media.getUserMedia).toHaveBeenCalledWith({ audio: true });
    expect(peerConnection.tracks).toHaveLength(1);
    expect(peerConnection.dataChannels[0].label).toBe("oai-events");
    expect(peerConnection.localDescription?.sdp).toBe("offer-sdp");
    expect(peerConnection.remoteDescription).toEqual({
      type: "answer",
      sdp: "answer-sdp"
    });
    expect(states).toEqual(["requesting_microphone", "connecting", "connected"]);
  });

  it("turns realtime data channel events into transcript entries", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response("answer-sdp", { status: 200 }))
    );
    const peerConnection = new FakePeerConnection();
    const transcripts: OpenAIRealtimeTranscriptEntry[] = [];
    const client = createOpenAIRealtimeWebRtcClient({
      sessionId: "33333333-3333-3333-3333-333333333333",
      mediaDevices: fakeMediaDevices(),
      peerConnectionFactory: () => peerConnection as unknown as RTCPeerConnection,
      audioElementFactory: () => fakeAudioElement()
    });
    client.onTranscript((entry) => transcripts.push(entry));
    await client.start();

    peerConnection.dataChannels[0].emit({
      type: "conversation.item.input_audio_transcription.completed",
      event_id: "evt-user",
      transcript: "Mene zvaty Dima"
    });
    peerConnection.dataChannels[0].emit({
      type: "response.audio_transcript.delta",
      event_id: "evt-assistant-1",
      delta: "Vitaiu"
    });
    peerConnection.dataChannels[0].emit({
      type: "response.audio_transcript.delta",
      event_id: "evt-assistant-2",
      delta: ", Dima"
    });
    peerConnection.dataChannels[0].emit({
      type: "response.audio_transcript.done",
      event_id: "evt-assistant-3",
      transcript: "Vitaiu, Dima"
    });

    expect(transcripts[0]).toMatchObject({
      id: "evt-user",
      role: "user",
      text: "Mene zvaty Dima",
      isFinal: true
    });
    expect(transcripts.at(-1)).toMatchObject({
      role: "assistant",
      text: "Vitaiu, Dima",
      isFinal: true
    });
  });

  it("stops the data channel, peer connection, and microphone tracks", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response("answer-sdp", { status: 200 }))
    );
    const media = fakeMediaDevices();
    const peerConnection = new FakePeerConnection();
    const states: string[] = [];
    const client = createOpenAIRealtimeWebRtcClient({
      sessionId: "44444444-4444-4444-4444-444444444444",
      mediaDevices: media,
      peerConnectionFactory: () => peerConnection as unknown as RTCPeerConnection,
      audioElementFactory: () => fakeAudioElement()
    });
    client.onStateChange((state) => states.push(state.status));
    await client.start();

    client.stop();

    expect(peerConnection.dataChannels[0].closed).toBe(true);
    expect(peerConnection.closed).toBe(true);
    expect(media.track.stop).toHaveBeenCalled();
    expect(states.at(-1)).toBe("stopped");
  });

  it("does not continue startup after stop while microphone permission is pending", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response("answer-sdp", { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);
    const media = pendingFakeMediaDevices();
    const peerConnectionFactory = vi.fn(() => new FakePeerConnection() as unknown as RTCPeerConnection);
    const states: string[] = [];
    const client = createOpenAIRealtimeWebRtcClient({
      sessionId: "55555555-5555-5555-5555-555555555555",
      mediaDevices: media,
      peerConnectionFactory,
      audioElementFactory: () => fakeAudioElement()
    });
    client.onStateChange((state) => states.push(state.status));

    const startPromise = client.start();
    client.stop();
    media.resolve();
    await startPromise;

    expect(peerConnectionFactory).not.toHaveBeenCalled();
    expect(fetchMock).not.toHaveBeenCalled();
    expect(media.track.stop).toHaveBeenCalled();
    expect(states).toEqual(["requesting_microphone", "stopped"]);
  });
});

function fakeMediaDevices() {
  const track = { stop: vi.fn() };
  const stream = {
    getAudioTracks: vi.fn(() => [track]),
    getTracks: vi.fn(() => [track])
  };

  return {
    track,
    getUserMedia: vi.fn(async () => stream as unknown as MediaStream)
  };
}

function pendingFakeMediaDevices() {
  const track = { stop: vi.fn() };
  const stream = {
    getAudioTracks: vi.fn(() => [track]),
    getTracks: vi.fn(() => [track])
  };
  const media: {
    track: typeof track;
    getUserMedia: ReturnType<typeof vi.fn>;
    promise: Promise<MediaStream>;
    resolve: () => void;
  } = {
    track,
    getUserMedia: vi.fn(() => media.promise as Promise<MediaStream>),
    promise: Promise.resolve(stream as unknown as MediaStream),
    resolve: () => {
    }
  };
  let resolvePromise!: () => void;
  media.promise = new Promise<MediaStream>((resolve) => {
    resolvePromise = () => resolve(stream as unknown as MediaStream);
  });
  media.resolve = resolvePromise;

  return media;
}

function fakeAudioElement() {
  return {
    autoplay: false,
    srcObject: null
  } as unknown as HTMLAudioElement;
}

class FakePeerConnection {
  public dataChannels: FakeDataChannel[] = [];

  public localDescription: RTCSessionDescriptionInit | null = null;

  public remoteDescription: RTCSessionDescriptionInit | null = null;

  public tracks: Array<{ track: MediaStreamTrack; stream: MediaStream }> = [];

  public closed = false;

  createDataChannel(label: string) {
    const dataChannel = new FakeDataChannel(label);
    this.dataChannels.push(dataChannel);

    return dataChannel as unknown as RTCDataChannel;
  }

  addTrack(track: MediaStreamTrack, stream: MediaStream) {
    this.tracks.push({ track, stream });
  }

  async createOffer() {
    return {
      type: "offer",
      sdp: "offer-sdp"
    } as RTCSessionDescriptionInit;
  }

  async setLocalDescription(description: RTCSessionDescriptionInit) {
    this.localDescription = description;
  }

  async setRemoteDescription(description: RTCSessionDescriptionInit) {
    this.remoteDescription = description;
  }

  close() {
    this.closed = true;
  }
}

class FakeDataChannel {
  public closed = false;

  public onmessage: ((event: MessageEvent<string>) => void) | null = null;

  constructor(public readonly label: string) {
  }

  send() {
  }

  close() {
    this.closed = true;
  }

  emit(payload: unknown) {
    this.onmessage?.({ data: JSON.stringify(payload) } as MessageEvent<string>);
  }
}
