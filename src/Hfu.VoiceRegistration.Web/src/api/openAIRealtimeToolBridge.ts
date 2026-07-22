import {
  clearRegistrationFields,
  completeRegistration,
  confirmRegistrationFields,
  getRegistrationState,
  markFieldsForClarification,
  updateRegistrationFields
} from "./registrationClient";
import type {
  CompleteRegistrationRequest,
  FieldNamesRequest,
  MarkFieldsForClarificationRequest,
  ProblemDetails,
  RegistrationFieldUpdate,
  RegistrationToolResult,
  UpdateRegistrationFieldsRequest
} from "./registrationTypes";
import type {
  OpenAIRealtimeToolCall,
  OpenAIRealtimeWebRtcClient
} from "./openAIRealtimeTypes";

export type OpenAIRealtimeToolActivityStatus = "running" | "completed" | "error";

export interface OpenAIRealtimeToolActivity {
  id: string;
  callId: string;
  name: string;
  status: OpenAIRealtimeToolActivityStatus;
  message?: string;
  startedAt: string;
  completedAt?: string;
}

export interface OpenAIRealtimeRegistrationToolClient {
  updateRegistrationFields: (
    sessionId: string,
    request: UpdateRegistrationFieldsRequest,
    baseUrl?: string
  ) => Promise<RegistrationToolResult>;
  confirmRegistrationFields: (
    sessionId: string,
    request: FieldNamesRequest,
    baseUrl?: string
  ) => Promise<RegistrationToolResult>;
  markFieldsForClarification: (
    sessionId: string,
    request: MarkFieldsForClarificationRequest,
    baseUrl?: string
  ) => Promise<RegistrationToolResult>;
  clearRegistrationFields: (
    sessionId: string,
    request: FieldNamesRequest,
    baseUrl?: string
  ) => Promise<RegistrationToolResult>;
  getRegistrationState: (
    sessionId: string,
    baseUrl?: string
  ) => Promise<RegistrationToolResult>;
  completeRegistration: (
    sessionId: string,
    request: CompleteRegistrationRequest,
    baseUrl?: string
  ) => Promise<RegistrationToolResult>;
}

export interface OpenAIRealtimeToolBridgeOptions {
  sessionId: string;
  baseUrl?: string;
  voiceClient: Pick<OpenAIRealtimeWebRtcClient, "sendEvent" | "onToolCall">;
  registrationTools?: OpenAIRealtimeRegistrationToolClient;
  onActivity?: (activity: OpenAIRealtimeToolActivity) => void;
  onToolResult?: (result: RegistrationToolResult) => void;
}

export interface OpenAIRealtimeToolBridge {
  dispose: () => void;
}

const defaultRegistrationTools: OpenAIRealtimeRegistrationToolClient = {
  updateRegistrationFields,
  confirmRegistrationFields,
  markFieldsForClarification,
  clearRegistrationFields,
  getRegistrationState,
  completeRegistration
};

export function createOpenAIRealtimeToolBridge(
  options: OpenAIRealtimeToolBridgeOptions
): OpenAIRealtimeToolBridge {
  const registrationTools = options.registrationTools ?? defaultRegistrationTools;
  const handledCallIds = new Set<string>();
  let disposed = false;

  const unsubscribe = options.voiceClient.onToolCall((toolCall) => {
    if (handledCallIds.has(toolCall.callId)) {
      return;
    }

    handledCallIds.add(toolCall.callId);
    void handleToolCall(toolCall);
  });

  async function handleToolCall(toolCall: OpenAIRealtimeToolCall) {
    const startedAt = new Date().toISOString();
    emitActivity(toolCall, {
      status: "running",
      startedAt
    });

    let result: RegistrationToolResult;
    let status: OpenAIRealtimeToolActivityStatus = "completed";
    let message: string | undefined;

    try {
      result = await dispatchToolCall(toolCall, registrationTools, options.sessionId, options.baseUrl);
      options.onToolResult?.(result);
      message = result.errors.length > 0
        ? `${result.errors.length} backend validation error(s).`
        : undefined;
    } catch (error) {
      result = errorToolResult(error);
      status = "error";
      message = result.errors[0]?.message;
    }

    if (disposed) {
      return;
    }

    options.voiceClient.sendEvent({
      type: "conversation.item.create",
      item: {
        type: "function_call_output",
        call_id: toolCall.callId,
        output: JSON.stringify(result)
      }
    });
    options.voiceClient.sendEvent({ type: "response.create" });

    emitActivity(toolCall, {
      status,
      startedAt,
      completedAt: new Date().toISOString(),
      message
    });
  }

  function emitActivity(
    toolCall: OpenAIRealtimeToolCall,
    activity: Pick<OpenAIRealtimeToolActivity, "status" | "startedAt" | "completedAt" | "message">
  ) {
    options.onActivity?.({
      id: toolCall.id,
      callId: toolCall.callId,
      name: toolCall.name,
      ...activity
    });
  }

  return {
    dispose() {
      disposed = true;
      unsubscribe();
      handledCallIds.clear();
    }
  };
}

async function dispatchToolCall(
  toolCall: OpenAIRealtimeToolCall,
  registrationTools: OpenAIRealtimeRegistrationToolClient,
  sessionId: string,
  baseUrl = ""
): Promise<RegistrationToolResult> {
  const args = parseArguments(toolCall.argumentsJson);

  switch (toolCall.name) {
    case "update_registration_fields":
      return registrationTools.updateRegistrationFields(
        sessionId,
        updateRegistrationFieldsRequest(args),
        baseUrl
      );
    case "confirm_registration_fields":
      return registrationTools.confirmRegistrationFields(
        sessionId,
        fieldNamesRequest(args),
        baseUrl
      );
    case "mark_fields_for_clarification":
      return registrationTools.markFieldsForClarification(
        sessionId,
        markFieldsForClarificationRequest(args),
        baseUrl
      );
    case "clear_registration_fields":
      return registrationTools.clearRegistrationFields(
        sessionId,
        fieldNamesRequest(args),
        baseUrl
      );
    case "get_registration_state":
      return registrationTools.getRegistrationState(sessionId, baseUrl);
    case "complete_registration":
      return registrationTools.completeRegistration(
        sessionId,
        completeRegistrationRequest(args),
        baseUrl
      );
    default:
      throw new ToolBridgeError(
        "RealtimeToolUnknown",
        `Realtime tool '${toolCall.name}' is not supported by this client.`
      );
  }
}

function parseArguments(argumentsJson: string): Record<string, unknown> {
  try {
    const parsed = JSON.parse(argumentsJson) as unknown;
    if (isRecord(parsed)) {
      return parsed;
    }
  } catch {
    // The structured error below is enough for both UI diagnostics and model continuation.
  }

  throw invalidArguments("Tool arguments must be a JSON object.");
}

function updateRegistrationFieldsRequest(args: Record<string, unknown>): UpdateRegistrationFieldsRequest {
  const fields = args.fields;
  if (!Array.isArray(fields) || fields.length === 0) {
    throw invalidArguments("update_registration_fields requires a non-empty fields array.");
  }

  return {
    fields: fields.map((entry) => {
      if (!isRecord(entry) || typeof entry.name !== "string" || !hasOwn(entry, "value")) {
        throw invalidArguments("Each field update requires name and value.");
      }

      const update: RegistrationFieldUpdate = {
        name: entry.name,
        value: entry.value
      };
      if (typeof entry.rawValue === "string" || entry.rawValue === null) {
        update.rawValue = entry.rawValue;
      }

      return update;
    })
  };
}

function fieldNamesRequest(args: Record<string, unknown>): FieldNamesRequest {
  return {
    fieldNames: fieldNames(args)
  };
}

function markFieldsForClarificationRequest(
  args: Record<string, unknown>
): MarkFieldsForClarificationRequest {
  const request: MarkFieldsForClarificationRequest = {
    fieldNames: fieldNames(args)
  };
  if (typeof args.reason === "string" || args.reason === null) {
    request.reason = args.reason;
  }

  return request;
}

function completeRegistrationRequest(args: Record<string, unknown>): CompleteRegistrationRequest {
  if (typeof args.personalDataConsent !== "boolean" || typeof args.registrationConfirmed !== "boolean") {
    throw invalidArguments("complete_registration requires boolean personalDataConsent and registrationConfirmed.");
  }

  return {
    personalDataConsent: args.personalDataConsent,
    registrationConfirmed: args.registrationConfirmed
  };
}

function fieldNames(args: Record<string, unknown>): string[] {
  if (
    !Array.isArray(args.fieldNames)
    || args.fieldNames.length === 0
    || args.fieldNames.some((fieldName) => typeof fieldName !== "string")
  ) {
    throw invalidArguments("Tool requires a non-empty fieldNames string array.");
  }

  return args.fieldNames;
}

function errorToolResult(error: unknown): RegistrationToolResult {
  if (error instanceof ToolBridgeError) {
    return structuredError(error.code, error.message);
  }

  return structuredError(
    "RealtimeToolDispatchFailed",
    errorMessage(error)
  );
}

function structuredError(code: string, message: string): RegistrationToolResult {
  return {
    succeeded: false,
    state: null,
    errors: [
      {
        code,
        field: null,
        message,
        suggestions: null
      }
    ],
    completion: null
  };
}

function invalidArguments(message: string): ToolBridgeError {
  return new ToolBridgeError("RealtimeToolArgumentsInvalid", message);
}

function errorMessage(error: unknown): string {
  if (isProblemDetails(error)) {
    return error.detail ?? error.title;
  }

  return error instanceof Error
    ? error.message
    : "Realtime tool dispatch failed.";
}

function isProblemDetails(error: unknown): error is ProblemDetails {
  return isRecord(error)
    && typeof error.title === "string"
    && typeof error.status === "number";
}

function hasOwn(value: Record<string, unknown>, propertyName: string): boolean {
  return Object.prototype.hasOwnProperty.call(value, propertyName);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

class ToolBridgeError extends Error {
  public constructor(
    public readonly code: string,
    message: string)
  {
    super(message);
  }
}
