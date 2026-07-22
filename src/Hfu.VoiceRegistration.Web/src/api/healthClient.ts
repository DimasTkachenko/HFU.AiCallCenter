export interface HealthResponse {
  status: string;
  service: string;
  timestampUtc: string;
  version?: string | null;
}

export async function fetchHealth(baseUrl = ""): Promise<HealthResponse> {
  const normalizedBaseUrl = baseUrl.replace(/\/$/, "");
  const response = await fetch(`${normalizedBaseUrl}/health`, {
    headers: { Accept: "application/json" }
  });

  if (!response.ok) {
    throw new Error(`Backend health check failed with status ${response.status}.`);
  }

  return response.json() as Promise<HealthResponse>;
}
