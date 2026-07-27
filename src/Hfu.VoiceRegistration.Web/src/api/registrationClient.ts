import type {
  CompleteRegistrationRequest,
  ConversationSessionResponse,
  FieldNamesRequest,
  MarkFieldsForClarificationRequest,
  ProblemDetails,
  RegionReference,
  RegistrationToolResult,
  UpdateRegistrationFieldsRequest
} from "./registrationTypes";

const acceptJsonHeaders = { Accept: "application/json" };
const postJsonHeaders = { Accept: "application/json", "Content-Type": "application/json" };

export async function createConversationSession(
  baseUrl = ""
): Promise<ConversationSessionResponse> {
  return post<ConversationSessionResponse>(
    "/api/conversation-sessions",
    {},
    baseUrl
  );
}

export async function getConversationSession(
  sessionId: string,
  baseUrl = ""
): Promise<ConversationSessionResponse> {
  return get<ConversationSessionResponse>(
    `/api/conversation-sessions/${sessionId}`,
    baseUrl
  );
}

export async function abandonConversationSession(
  sessionId: string,
  baseUrl = ""
): Promise<ConversationSessionResponse> {
  return post<ConversationSessionResponse>(
    `/api/conversation-sessions/${sessionId}/abandon`,
    {},
    baseUrl
  );
}

export async function fetchRegions(baseUrl = ""): Promise<RegionReference[]> {
  const response = await get<{ regions: RegionReference[] }>(
    "/api/reference-data/regions",
    baseUrl
  );

  return response.regions;
}

export async function updateRegistrationFields(
  sessionId: string,
  request: UpdateRegistrationFieldsRequest,
  baseUrl = ""
): Promise<RegistrationToolResult> {
  return toolPost(sessionId, "update-registration-fields", request, baseUrl);
}

export async function confirmRegistrationFields(
  sessionId: string,
  request: FieldNamesRequest,
  baseUrl = ""
): Promise<RegistrationToolResult> {
  return toolPost(sessionId, "confirm-registration-fields", request, baseUrl);
}

export async function markFieldsForClarification(
  sessionId: string,
  request: MarkFieldsForClarificationRequest,
  baseUrl = ""
): Promise<RegistrationToolResult> {
  return toolPost(sessionId, "mark-fields-for-clarification", request, baseUrl);
}

export async function clearRegistrationFields(
  sessionId: string,
  request: FieldNamesRequest,
  baseUrl = ""
): Promise<RegistrationToolResult> {
  return toolPost(sessionId, "clear-registration-fields", request, baseUrl);
}

export async function getRegistrationState(
  sessionId: string,
  baseUrl = ""
): Promise<RegistrationToolResult> {
  return toolPost(sessionId, "get-registration-state", {}, baseUrl);
}

export async function completeRegistration(
  sessionId: string,
  request: CompleteRegistrationRequest,
  baseUrl = ""
): Promise<RegistrationToolResult> {
  return toolPost(sessionId, "complete-registration", request, baseUrl);
}

async function toolPost(
  sessionId: string,
  toolPath: string,
  body: unknown,
  baseUrl: string
): Promise<RegistrationToolResult> {
  return post<RegistrationToolResult>(
    `/api/conversation-sessions/${sessionId}/tools/${toolPath}`,
    body,
    baseUrl
  );
}

async function get<T>(path: string, baseUrl: string): Promise<T> {
  const response = await fetch(toUrl(path, baseUrl), {
    headers: acceptJsonHeaders
  });

  return parseResponse<T>(response);
}

async function post<T>(
  path: string,
  body: unknown,
  baseUrl: string
): Promise<T> {
  const response = await fetch(toUrl(path, baseUrl), {
    method: "POST",
    headers: postJsonHeaders,
    body: JSON.stringify(body)
  });

  return parseResponse<T>(response);
}

async function parseResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    return response.json() as Promise<T>;
  }

  const problem = await parseProblemDetails(response);
  throw problem;
}

async function parseProblemDetails(response: Response): Promise<ProblemDetails> {
  try {
    const parsed = await response.json() as Partial<ProblemDetails>;
    return {
      title: parsed.title ?? "HTTP request failed",
      status: parsed.status ?? response.status,
      detail: parsed.detail
    };
  } catch {
    return {
      title: "HTTP request failed",
      status: response.status,
      detail: `Request failed with status ${response.status}.`
    };
  }
}

function toUrl(path: string, baseUrl: string): string {
  return `${baseUrl.replace(/\/$/, "")}${path}`;
}
