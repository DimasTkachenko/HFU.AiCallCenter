# Stage 10 OpenAI Realtime WebRTC Design

## Goal

Stage 10 adds the first real OpenAI voice connection. A user can create or restore an existing conversation session, start a browser WebRTC call, speak through the microphone, hear model audio, and see a compact transcript/event stream.

The AI still does not update the registration draft. Registration tool calls, the OpenAI tool-call bridge, and the full registration system prompt remain deferred to later stages.

## Confirmed Approach

Use the OpenAI Realtime unified WebRTC interface.

`Hfu.VoiceRegistration.Web` creates the `RTCPeerConnection`, microphone track, remote audio element, and `oai-events` data channel. It sends the SDP offer to `Hfu.VoiceRegistration.Api`. The backend authenticates to OpenAI with the server-side API key, calls `POST /v1/realtime/calls`, and returns the SDP answer.

This keeps the permanent OpenAI API key out of the frontend bundle. Local development uses `appsettings.json` or `appsettings.Development.json`; deployment can override the same configuration with environment variables such as `OpenAI__ApiKey`.

## Backend Design

Add endpoint:

```text
POST /api/conversation-sessions/{sessionId}/realtime/calls
Content-Type: application/sdp
Accept: application/sdp
```

Request body is the raw SDP offer. Response body is the raw SDP answer with `Content-Type: application/sdp`.

The endpoint validates:

- session exists, otherwise `404` Problem Details;
- session is not `Completed` or `Abandoned`, otherwise `409` Problem Details;
- SDP offer is not blank, otherwise `400` Problem Details;
- OpenAI config contains an API key, otherwise `500` Problem Details with a `Realtime configuration failure` title.

The OpenAI request uses multipart form data with:

- `sdp`: raw SDP offer;
- `session`: JSON session configuration.

Default session configuration:

```json
{
  "type": "realtime",
  "model": "gpt-realtime-2.1",
  "instructions": "You are connected to a local HFU voice registration demo. Keep the conversation short. Registration tools are not connected yet, so do not claim to save or submit registration data.",
  "audio": {
    "input": {
      "transcription": {
        "model": "gpt-realtime-whisper"
      },
      "turn_detection": {
        "type": "server_vad"
      }
    },
    "output": {
      "voice": "marin"
    }
  }
}
```

Configuration keys:

- `OpenAI:ApiKey`;
- `OpenAI:BaseUrl`, default `https://api.openai.com/v1`;
- `OpenAI:RealtimeModel`, default `gpt-realtime-2.1`;
- `OpenAI:RealtimeVoice`, default `marin`;
- `OpenAI:RealtimeInputTranscriptionModel`, default `gpt-realtime-whisper`;
- `OpenAI:RealtimeInstructions`.

Do not log the API key or full registration DTOs.

## Frontend Design

Add a focused WebRTC client wrapper:

```ts
createOpenAIRealtimeWebRtcClient({
  baseUrl?: string,
  sessionId: string,
  mediaDevices?: MediaDevices,
  peerConnectionFactory?: () => RTCPeerConnection,
  audioElementFactory?: () => HTMLAudioElement
})
```

The wrapper owns browser media resources and exposes:

- `start(): Promise<void>`;
- `stop(): void`;
- `sendEvent(event: unknown): void`;
- `onStateChange(handler): unsubscribe`;
- `onTranscript(handler): unsubscribe`;
- `onEvent(handler): unsubscribe`.

Connection flow:

1. `getUserMedia({ audio: true })`;
2. create `RTCPeerConnection`;
3. attach remote audio in `ontrack`;
4. add microphone audio track;
5. create `oai-events` data channel;
6. create local SDP offer;
7. POST offer to `/api/conversation-sessions/{sessionId}/realtime/calls`;
8. set remote SDP answer.

The UI adds a voice panel with Russian labels:

- start/stop voice buttons;
- voice state;
- recent Realtime events;
- compact transcript entries for user and assistant speech.

Known event types to translate into transcripts:

- `conversation.item.input_audio_transcription.completed` -> user transcript;
- `response.audio_transcript.delta` -> append assistant transcript;
- `response.audio_transcript.done` -> finalize assistant transcript.

Unknown Realtime events are shown only in diagnostics.

## Error Handling

Backend maps OpenAI non-success responses to Problem Details without exposing secrets. Frontend maps microphone, WebRTC, SDP, HTTP, and data channel failures into a visible voice error state while leaving the existing registration UI usable.

Stopping voice closes the data channel, closes the peer connection, stops microphone tracks, and clears the hidden remote audio element.

## Testing

Backend tests cover:

- missing session returns `404`;
- blank SDP returns `400`;
- completed or abandoned session returns `409`;
- missing OpenAI API key returns `500`;
- successful request forwards SDP and session config to OpenAI and returns SDP answer.

Frontend tests cover:

- API client posts raw SDP with `application/sdp` and parses SDP answer;
- WebRTC wrapper requests microphone, creates data channel, posts SDP offer, and sets remote answer;
- WebRTC wrapper stops tracks and closes the peer connection;
- transcript parser handles user transcript and assistant transcript delta/done events;
- App voice panel starts and stops the voice connection for the current session.

## Explicit Non-Goals

- No registration tool calls from OpenAI.
- No full registration system prompt.
- No completion through voice.
- No SIP.
- No Redis/backplane, EF Core, database packages, persistent storage, authentication, authorization, or production HFU integration.
- No OpenAI API key in the React app or frontend environment variables.
