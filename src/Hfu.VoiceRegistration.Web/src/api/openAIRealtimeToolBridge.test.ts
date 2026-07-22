import { describe, expect, it, vi } from "vitest";
import {
  createOpenAIRealtimeToolBridge,
  type OpenAIRealtimeRegistrationToolClient,
  type OpenAIRealtimeToolActivity
} from "./openAIRealtimeToolBridge";
import type { OpenAIRealtimeToolCall } from "./openAIRealtimeTypes";
import type { RegistrationToolResult } from "./registrationTypes";

const sessionId = "11111111-1111-1111-1111-111111111111";
const baseUrl = "http://api.test";

describe("createOpenAIRealtimeToolBridge", () => {
  it("dispatches every supported realtime tool call and returns function output", async () => {
    const cases: Array<{
      name: string;
      argumentsJson: string;
      clientMethod: keyof OpenAIRealtimeRegistrationToolClient;
      expectedRequest?: unknown;
    }> = [
      {
        name: "update_registration_fields",
        argumentsJson: JSON.stringify({
          fields: [{ name: "firstName", value: "Dimas", rawValue: "Dimas" }]
        }),
        clientMethod: "updateRegistrationFields",
        expectedRequest: {
          fields: [{ name: "firstName", value: "Dimas", rawValue: "Dimas" }]
        }
      },
      {
        name: "confirm_registration_fields",
        argumentsJson: JSON.stringify({ fieldNames: ["firstName"] }),
        clientMethod: "confirmRegistrationFields",
        expectedRequest: { fieldNames: ["firstName"] }
      },
      {
        name: "mark_fields_for_clarification",
        argumentsJson: JSON.stringify({
          fieldNames: ["currentRegion"],
          reason: "Region was ambiguous."
        }),
        clientMethod: "markFieldsForClarification",
        expectedRequest: {
          fieldNames: ["currentRegion"],
          reason: "Region was ambiguous."
        }
      },
      {
        name: "clear_registration_fields",
        argumentsJson: JSON.stringify({ fieldNames: ["email"] }),
        clientMethod: "clearRegistrationFields",
        expectedRequest: { fieldNames: ["email"] }
      },
      {
        name: "get_registration_state",
        argumentsJson: "{}",
        clientMethod: "getRegistrationState"
      },
      {
        name: "complete_registration",
        argumentsJson: JSON.stringify({
          personalDataConsent: true,
          registrationConfirmed: true
        }),
        clientMethod: "completeRegistration",
        expectedRequest: {
          personalDataConsent: true,
          registrationConfirmed: true
        }
      }
    ];

    for (const testCase of cases) {
      const voice = fakeVoiceClient();
      const tools = fakeRegistrationTools();
      const activities: OpenAIRealtimeToolActivity[] = [];
      const results: RegistrationToolResult[] = [];
      const bridge = createOpenAIRealtimeToolBridge({
        sessionId,
        baseUrl,
        voiceClient: voice.client,
        registrationTools: tools,
        onActivity: (activity) => activities.push(activity),
        onToolResult: (result) => results.push(result)
      });

      voice.emitToolCall(toolCall({
        callId: `call-${testCase.name}`,
        name: testCase.name,
        argumentsJson: testCase.argumentsJson
      }));
      await flushPromises();

      const method = tools[testCase.clientMethod] as ReturnType<typeof vi.fn>;
      if (testCase.expectedRequest) {
        expect(method).toHaveBeenCalledWith(sessionId, testCase.expectedRequest, baseUrl);
      } else {
        expect(method).toHaveBeenCalledWith(sessionId, baseUrl);
      }
      expect(voice.sentEvents).toEqual([
        {
          type: "conversation.item.create",
          item: {
            type: "function_call_output",
            call_id: `call-${testCase.name}`,
            output: JSON.stringify(successToolResult)
          }
        },
        { type: "response.create" }
      ]);
      expect(results).toEqual([successToolResult]);
      expect(activities.map((activity) => activity.status)).toEqual(["running", "completed"]);

      bridge.dispose();
    }
  });

  it("returns a structured error output for an unknown tool", async () => {
    const voice = fakeVoiceClient();
    const tools = fakeRegistrationTools();
    const activities: OpenAIRealtimeToolActivity[] = [];
    createOpenAIRealtimeToolBridge({
      sessionId,
      baseUrl,
      voiceClient: voice.client,
      registrationTools: tools,
      onActivity: (activity) => activities.push(activity)
    });

    voice.emitToolCall(toolCall({
      callId: "call-unknown",
      name: "delete_everything",
      argumentsJson: "{}"
    }));
    await flushPromises();

    expect(tools.updateRegistrationFields).not.toHaveBeenCalled();
    expect(voice.sentEvents[0]).toMatchObject({
      type: "conversation.item.create",
      item: {
        type: "function_call_output",
        call_id: "call-unknown"
      }
    });
    expect(JSON.parse((voice.sentEvents[0] as ToolOutputEvent).item.output)).toMatchObject({
      succeeded: false,
      errors: [{ code: "RealtimeToolUnknown" }]
    });
    expect(voice.sentEvents[1]).toEqual({ type: "response.create" });
    expect(activities.map((activity) => activity.status)).toEqual(["running", "error"]);
  });

  it("returns a structured error output for invalid arguments", async () => {
    const voice = fakeVoiceClient();
    const tools = fakeRegistrationTools();
    createOpenAIRealtimeToolBridge({
      sessionId,
      baseUrl,
      voiceClient: voice.client,
      registrationTools: tools
    });

    voice.emitToolCall(toolCall({
      callId: "call-invalid",
      name: "update_registration_fields",
      argumentsJson: "{\"fields\":\"not-an-array\"}"
    }));
    await flushPromises();

    expect(tools.updateRegistrationFields).not.toHaveBeenCalled();
    expect(JSON.parse((voice.sentEvents[0] as ToolOutputEvent).item.output)).toMatchObject({
      succeeded: false,
      errors: [{ code: "RealtimeToolArgumentsInvalid" }]
    });
    expect(voice.sentEvents[1]).toEqual({ type: "response.create" });
  });
});

const successToolResult: RegistrationToolResult = {
  succeeded: true,
  state: {
    sessionId,
    version: 2,
    fields: [],
    missingRequiredFields: [],
    fieldsRequiringClarification: [],
    fieldsAwaitingConfirmation: [],
    registrationCanBeCompleted: true,
    completionIssues: []
  },
  errors: [],
  completion: null
};

function fakeRegistrationTools(): OpenAIRealtimeRegistrationToolClient {
  return {
    updateRegistrationFields: vi.fn(async () => successToolResult),
    confirmRegistrationFields: vi.fn(async () => successToolResult),
    markFieldsForClarification: vi.fn(async () => successToolResult),
    clearRegistrationFields: vi.fn(async () => successToolResult),
    getRegistrationState: vi.fn(async () => successToolResult),
    completeRegistration: vi.fn(async () => successToolResult)
  };
}

function fakeVoiceClient() {
  const toolCallHandlers = new Set<(toolCall: OpenAIRealtimeToolCall) => void>();
  const sentEvents: unknown[] = [];

  return {
    sentEvents,
    client: {
      sendEvent: vi.fn((event: unknown) => {
        sentEvents.push(event);
      }),
      onToolCall: vi.fn((handler: (toolCall: OpenAIRealtimeToolCall) => void) => {
        toolCallHandlers.add(handler);

        return () => toolCallHandlers.delete(handler);
      })
    },
    emitToolCall(toolCall: OpenAIRealtimeToolCall) {
      for (const handler of toolCallHandlers) {
        handler(toolCall);
      }
    }
  };
}

function toolCall(overrides: Partial<OpenAIRealtimeToolCall>): OpenAIRealtimeToolCall {
  return {
    id: "evt-tool",
    callId: "call-tool",
    name: "get_registration_state",
    argumentsJson: "{}",
    receivedAt: "2026-07-22T12:00:00Z",
    ...overrides
  };
}

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0));
}

interface ToolOutputEvent {
  type: "conversation.item.create";
  item: {
    type: "function_call_output";
    call_id: string;
    output: string;
  };
}
