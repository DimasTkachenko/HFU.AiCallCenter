export interface ProblemDetails {
  title: string;
  status: number;
  detail?: string;
}

export interface ConversationSessionResponse {
  sessionId: string;
  status: ConversationSessionStatus;
  version: number;
  createdAt: string;
  lastActivityAt: string;
  realtimeConnectionId?: string | null;
  registrationResult?: RegistrationResult | null;
  state: RegistrationStateSnapshot;
  events: ConversationEventResponse[];
}

export type ConversationSessionStatus =
  | "Created"
  | "Connecting"
  | "Active"
  | "Completing"
  | "Completed"
  | "Failed"
  | "Abandoned";

export interface ConversationEventResponse {
  type: string;
  message: string;
  occurredAt: string;
}

export interface RegistrationStateSnapshot {
  sessionId: string;
  version: number;
  fields: RegistrationFieldSnapshot[];
  missingRequiredFields: string[];
  fieldsRequiringClarification: string[];
  fieldsAwaitingConfirmation: string[];
  registrationCanBeCompleted: boolean;
  completionIssues: RegistrationValidationIssue[];
}

export interface RegistrationFieldSnapshot {
  name: string;
  value: unknown;
  rawValue?: string | null;
  status: RegistrationFieldStatus;
  clarificationReason?: string | null;
  referenceId?: string | null;
}

export type RegistrationFieldStatus =
  | "Missing"
  | "Captured"
  | "Confirmed"
  | "NeedsClarification"
  | "Rejected";

export interface RegistrationValidationIssue {
  field: string;
  code: string;
  message: string;
}

export interface RegistrationToolResult {
  succeeded: boolean;
  state: RegistrationStateSnapshot | null;
  errors: RegistrationToolError[];
  completion?: RegistrationCompletionDetails | null;
}

export interface RegistrationToolError {
  code: string;
  field?: string | null;
  message: string;
  suggestions?: string[] | null;
}

export interface RegistrationCompletionDetails {
  finalRegistration: FinalRegistrationDto;
  registrationResult: RegistrationResult;
}

export interface FinalRegistrationDto {
  firstName: string;
  lastName: string;
  patronymic?: string | null;
  dateOfBirth: string;
  phoneNumber: string;
  email?: string | null;
  currentRegion: string;
  currentRegionReferenceId?: string | null;
  currentCity: string;
  actualAddress?: string | null;
  userCategory: string;
  regionBeforeWar?: string | null;
  regionBeforeWarReferenceId?: string | null;
  displacedCertificateYear?: number | null;
  personalDataConsent: boolean;
  registrationConfirmed: boolean;
}

export interface RegistrationResult {
  registrationId: string;
  completedAt: string;
}

export interface RegionReference {
  id: string;
  name: string;
  aliases: string[];
}

export interface RegistrationFieldUpdate {
  name: string;
  value: unknown;
  rawValue?: string | null;
}

export interface UpdateRegistrationFieldsRequest {
  fields: RegistrationFieldUpdate[];
}

export interface FieldNamesRequest {
  fieldNames: string[];
}

export interface MarkFieldsForClarificationRequest extends FieldNamesRequest {
  reason?: string | null;
}

export interface CompleteRegistrationRequest {
  personalDataConsent: boolean;
  registrationConfirmed: boolean;
}
