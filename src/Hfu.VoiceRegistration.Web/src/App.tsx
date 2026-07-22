import { useEffect, useMemo, useState } from "react";
import {
  abandonConversationSession,
  clearRegistrationFields,
  completeRegistration,
  confirmRegistrationFields,
  createConversationSession,
  fetchRegions,
  getConversationSession,
  getRegistrationState,
  markFieldsForClarification,
  updateRegistrationFields
} from "./api/registrationClient";
import { fetchHealth, type HealthResponse } from "./api/healthClient";
import type {
  ConversationSessionResponse,
  ProblemDetails,
  RegionReference,
  RegistrationFieldSnapshot,
  RegistrationStateSnapshot,
  RegistrationToolError,
  RegistrationToolResult
} from "./api/registrationTypes";

const sessionStorageKey = "hfu.voiceRegistration.sessionId";

type HealthState =
  | { kind: "loading" }
  | { kind: "healthy"; health: HealthResponse }
  | { kind: "error"; message: string };

type FormState = {
  firstName: string;
  lastName: string;
  patronymic: string;
  dateOfBirth: string;
  phoneNumber: string;
  email: string;
  currentRegion: string;
  currentCity: string;
  actualAddress: string;
  userCategory: string;
  regionBeforeWar: string;
  displacedCertificateYear: string;
  personalDataConsent: boolean;
  registrationConfirmed: boolean;
  clarificationFields: string;
  clarificationReason: string;
  clearFields: string;
};

const initialForm: FormState = {
  firstName: "",
  lastName: "",
  patronymic: "",
  dateOfBirth: "",
  phoneNumber: "",
  email: "",
  currentRegion: "",
  currentCity: "",
  actualAddress: "",
  userCategory: "",
  regionBeforeWar: "",
  displacedCertificateYear: "",
  personalDataConsent: false,
  registrationConfirmed: false,
  clarificationFields: "",
  clarificationReason: "",
  clearFields: ""
};

const demoForm: FormState = {
  ...initialForm,
  firstName: "Dimas",
  lastName: "Tkachenko",
  dateOfBirth: "1991-08-24",
  phoneNumber: "+380501112233",
  currentRegion: "Харківська область",
  currentCity: "Харків",
  userCategory: "Other"
};

const fieldLabels: Record<string, string> = {
  firstName: "Имя",
  lastName: "Фамилия",
  patronymic: "Отчество",
  dateOfBirth: "Дата рождения",
  phoneNumber: "Телефон",
  email: "Email",
  currentRegion: "Текущая область",
  currentCity: "Текущий город",
  actualAddress: "Фактический адрес",
  userCategory: "Категория",
  regionBeforeWar: "Область до войны",
  displacedCertificateYear: "Год справки ВПО",
  personalDataConsent: "Согласие",
  registrationConfirmed: "Подтверждение"
};

const orderedFieldNames = [
  "firstName",
  "lastName",
  "patronymic",
  "dateOfBirth",
  "phoneNumber",
  "email",
  "currentRegion",
  "currentCity",
  "actualAddress",
  "userCategory",
  "regionBeforeWar",
  "displacedCertificateYear",
  "personalDataConsent",
  "registrationConfirmed"
];

const userCategories = [
  { value: "Other", label: "Другая" },
  { value: "InternallyDisplacedPerson", label: "ВПО" },
  { value: "HasManyChildren", label: "Многодетная семья" },
  { value: "DisabledPerson", label: "Человек с инвалидностью" },
  { value: "MilitaryPerson", label: "Военнослужащий" },
  { value: "MilitaryPersonRelative", label: "Родственник военнослужащего" }
];

export default function App() {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "";
  const [healthState, setHealthState] = useState<HealthState>({ kind: "loading" });
  const [regions, setRegions] = useState<RegionReference[]>([]);
  const [session, setSession] = useState<ConversationSessionResponse | null>(null);
  const [registrationState, setRegistrationState] = useState<RegistrationStateSnapshot | null>(null);
  const [lastToolResult, setLastToolResult] = useState<RegistrationToolResult | null>(null);
  const [problem, setProblem] = useState<ProblemDetails | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [isBusy, setIsBusy] = useState(false);
  const [form, setForm] = useState<FormState>(initialForm);

  useEffect(() => {
    let isActive = true;

    fetchHealth(apiBaseUrl)
      .then((health) => {
        if (isActive) {
          setHealthState({ kind: "healthy", health });
        }
      })
      .catch((error: unknown) => {
        if (isActive) {
          setHealthState({ kind: "error", message: errorMessage(error) });
        }
      });

    fetchRegions(apiBaseUrl)
      .then((loadedRegions) => {
        if (isActive) {
          setRegions(loadedRegions);
        }
      })
      .catch((error: unknown) => {
        if (isActive) {
          setProblem(toProblem(error, "Не удалось загрузить справочник регионов."));
        }
      });

    const storedSessionId = localStorage.getItem(sessionStorageKey);
    if (storedSessionId) {
      getConversationSession(storedSessionId, apiBaseUrl)
        .then((restoredSession) => {
          if (isActive) {
            applySession(restoredSession);
            setNotice("Сессия восстановлена");
          }
        })
        .catch((error: unknown) => {
          if (isActive) {
            localStorage.removeItem(sessionStorageKey);
            setProblem(toProblem(error, "Не удалось восстановить сессию."));
          }
        });
    }

    return () => {
      isActive = false;
    };
  }, [apiBaseUrl]);

  const currentSessionId = session?.sessionId ?? null;
  const fieldMap = useMemo(() => {
    const map = new Map<string, RegistrationFieldSnapshot>();
    for (const field of registrationState?.fields ?? []) {
      map.set(field.name, field);
    }

    return map;
  }, [registrationState]);

  function applySession(nextSession: ConversationSessionResponse) {
    setSession(nextSession);
    setRegistrationState(nextSession.state);
  }

  function applyToolResult(result: RegistrationToolResult) {
    setLastToolResult(result);
    if (result.state) {
      setRegistrationState(result.state);
    }
  }

  async function runAction(action: () => Promise<void>) {
    setIsBusy(true);
    setProblem(null);
    setNotice(null);
    try {
      await action();
    } catch (error: unknown) {
      setProblem(toProblem(error, "Запрос к backend завершился ошибкой."));
    } finally {
      setIsBusy(false);
    }
  }

  async function handleCreateSession() {
    await runAction(async () => {
      const created = await createConversationSession(apiBaseUrl);
      localStorage.setItem(sessionStorageKey, created.sessionId);
      applySession(created);
      setLastToolResult(null);
      setNotice("Сессия создана");
    });
  }

  async function handleRefreshSession() {
    if (!currentSessionId) {
      return;
    }

    await runAction(async () => {
      const refreshed = await getConversationSession(currentSessionId, apiBaseUrl);
      applySession(refreshed);
      setNotice("Состояние обновлено");
    });
  }

  async function handleAbandonSession() {
    if (!currentSessionId) {
      return;
    }

    await runAction(async () => {
      const abandoned = await abandonConversationSession(currentSessionId, apiBaseUrl);
      applySession(abandoned);
      localStorage.removeItem(sessionStorageKey);
      setNotice("Сессия завершена вручную");
    });
  }

  async function handleUpdateFields() {
    if (!currentSessionId) {
      return;
    }

    await runAction(async () => {
      const result = await updateRegistrationFields(
        currentSessionId,
        { fields: buildFieldUpdates(form) },
        apiBaseUrl
      );
      applyToolResult(result);
    });
  }

  async function handleConfirmFields() {
    if (!currentSessionId) {
      return;
    }

    await runAction(async () => {
      const result = await confirmRegistrationFields(
        currentSessionId,
        { fieldNames: buildConfirmFieldNames(form, registrationState) },
        apiBaseUrl
      );
      applyToolResult(result);
    });
  }

  async function handleMarkClarification() {
    if (!currentSessionId) {
      return;
    }

    await runAction(async () => {
      const result = await markFieldsForClarification(
        currentSessionId,
        {
          fieldNames: parseFieldNames(form.clarificationFields),
          reason: emptyToUndefined(form.clarificationReason)
        },
        apiBaseUrl
      );
      applyToolResult(result);
    });
  }

  async function handleClearFields() {
    if (!currentSessionId) {
      return;
    }

    await runAction(async () => {
      const result = await clearRegistrationFields(
        currentSessionId,
        { fieldNames: parseFieldNames(form.clearFields) },
        apiBaseUrl
      );
      applyToolResult(result);
    });
  }

  async function handleGetState() {
    if (!currentSessionId) {
      return;
    }

    await runAction(async () => {
      const result = await getRegistrationState(currentSessionId, apiBaseUrl);
      applyToolResult(result);
    });
  }

  async function handleCompleteRegistration() {
    if (!currentSessionId) {
      return;
    }

    await runAction(async () => {
      const result = await completeRegistration(
        currentSessionId,
        {
          personalDataConsent: form.personalDataConsent,
          registrationConfirmed: form.registrationConfirmed
        },
        apiBaseUrl
      );
      applyToolResult(result);
      if (result.completion) {
        const completion = result.completion;
        setSession((current) => current
          ? {
              ...current,
              status: "Completed",
              version: result.state?.version ?? current.version,
              registrationResult: completion.registrationResult,
              state: result.state ?? current.state
            }
          : current);
        setNotice("Регистрация завершена");
      }
    });
  }

  function updateForm<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  return (
    <main className="shell">
      <section className="workspace" aria-labelledby="page-title">
        <header className="masthead">
          <div>
            <p className="eyebrow">HFU PoC</p>
            <h1 id="page-title">Демо регистрации HFU</h1>
          </div>
          <StatusPill healthState={healthState} />
        </header>

        <section className="top-grid" aria-label="Состояние сервиса и сессии">
          <HealthPanel healthState={healthState} />
          <SessionPanel
            session={session}
            notice={notice}
            isBusy={isBusy}
            onCreateSession={handleCreateSession}
            onRefreshSession={handleRefreshSession}
            onAbandonSession={handleAbandonSession}
          />
        </section>

        {problem ? <ProblemPanel problem={problem} /> : null}

        <section className="layout-grid" aria-label="Рабочая область регистрации">
          <section className="panel form-panel" aria-labelledby="registration-form-title">
            <div className="panel-header">
              <h2 id="registration-form-title">Анкета</h2>
              <button type="button" className="secondary-button" onClick={() => setForm(demoForm)}>
                Демо-данные
              </button>
            </div>

            <div className="field-grid">
              <TextInput label="Имя" value={form.firstName} onChange={(value) => updateForm("firstName", value)} />
              <TextInput label="Фамилия" value={form.lastName} onChange={(value) => updateForm("lastName", value)} />
              <TextInput label="Отчество" value={form.patronymic} onChange={(value) => updateForm("patronymic", value)} />
              <TextInput label="Дата рождения" type="date" value={form.dateOfBirth} onChange={(value) => updateForm("dateOfBirth", value)} />
              <TextInput label="Телефон" value={form.phoneNumber} onChange={(value) => updateForm("phoneNumber", value)} />
              <TextInput label="Email" type="email" value={form.email} onChange={(value) => updateForm("email", value)} />
              <SelectInput
                label="Текущая область"
                value={form.currentRegion}
                onChange={(value) => updateForm("currentRegion", value)}
                options={regions.map((region) => ({ value: region.name, label: region.name }))}
              />
              <TextInput label="Текущий город" value={form.currentCity} onChange={(value) => updateForm("currentCity", value)} />
              <TextInput label="Фактический адрес" value={form.actualAddress} onChange={(value) => updateForm("actualAddress", value)} />
              <SelectInput
                label="Категория"
                value={form.userCategory}
                onChange={(value) => updateForm("userCategory", value)}
                options={userCategories}
              />
              <SelectInput
                label="Область до войны"
                value={form.regionBeforeWar}
                onChange={(value) => updateForm("regionBeforeWar", value)}
                options={regions.map((region) => ({
                  value: region.name,
                  label: `${region.name} до войны`
                }))}
              />
              <TextInput
                label="Год справки ВПО"
                type="number"
                value={form.displacedCertificateYear}
                onChange={(value) => updateForm("displacedCertificateYear", value)}
              />
            </div>

            <div className="checkbox-row">
              <label>
                <input
                  type="checkbox"
                  checked={form.personalDataConsent}
                  onChange={(event) => updateForm("personalDataConsent", event.target.checked)}
                />
                Согласие на обработку данных
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={form.registrationConfirmed}
                  onChange={(event) => updateForm("registrationConfirmed", event.target.checked)}
                />
                Финальное подтверждение
              </label>
            </div>
          </section>

          <section className="panel actions-panel" aria-labelledby="actions-title">
            <div className="panel-header">
              <h2 id="actions-title">Инструменты регистрации</h2>
            </div>

            <div className="action-stack">
              <button type="button" onClick={handleUpdateFields} disabled={!currentSessionId}>
                Сохранить поля
              </button>
              <button type="button" onClick={handleConfirmFields} disabled={!currentSessionId}>
                Подтвердить заполненные
              </button>
              <button type="button" onClick={handleGetState} disabled={!currentSessionId}>
                Обновить состояние
              </button>
              <button type="button" className="primary-button" onClick={handleCompleteRegistration} disabled={!currentSessionId}>
                Завершить регистрацию
              </button>
            </div>

            <div className="developer-grid">
              <TextInput
                label="Поля для уточнения"
                value={form.clarificationFields}
                onChange={(value) => updateForm("clarificationFields", value)}
              />
              <TextInput
                label="Причина уточнения"
                value={form.clarificationReason}
                onChange={(value) => updateForm("clarificationReason", value)}
              />
              <button type="button" onClick={handleMarkClarification} disabled={!currentSessionId}>
                Уточнить поля
              </button>
              <TextInput
                label="Поля для очистки"
                value={form.clearFields}
                onChange={(value) => updateForm("clearFields", value)}
              />
              <button type="button" onClick={handleClearFields} disabled={!currentSessionId}>
                Очистить поля
              </button>
            </div>
          </section>
        </section>

        <section className="layout-grid layout-grid--wide" aria-label="Результаты backend">
          <RegistrationStatePanel state={registrationState} fieldMap={fieldMap} />
          <ToolFeedbackPanel result={lastToolResult} />
          <CompletionPanel result={lastToolResult} />
        </section>
      </section>
    </main>
  );
}

function StatusPill({ healthState }: { healthState: HealthState }) {
  if (healthState.kind === "healthy") {
    return <span className="status-pill status-pill--healthy">API онлайн</span>;
  }

  if (healthState.kind === "error") {
    return <span className="status-pill status-pill--error">API недоступен</span>;
  }

  return <span className="status-pill">проверка</span>;
}

function HealthPanel({ healthState }: { healthState: HealthState }) {
  return (
    <section className="panel" aria-label="Состояние backend">
      <div className="panel-header">
        <h2>Backend API</h2>
      </div>
      {healthState.kind === "loading" ? (
        <p className="state-message">Проверка...</p>
      ) : null}
      {healthState.kind === "error" ? (
        <p className="state-message state-message--error">{healthState.message}</p>
      ) : null}
      {healthState.kind === "healthy" ? (
        <dl className="metric-grid">
          <Metric label="Статус" value={translateHealthStatus(healthState.health.status)} tone="good" />
          <Metric label="Сервис" value={healthState.health.service} />
          <Metric label="Версия" value={healthState.health.version ?? "локальная"} />
        </dl>
      ) : null}
    </section>
  );
}

function SessionPanel({
  session,
  notice,
  isBusy,
  onCreateSession,
  onRefreshSession,
  onAbandonSession
}: {
  session: ConversationSessionResponse | null;
  notice: string | null;
  isBusy: boolean;
  onCreateSession: () => void;
  onRefreshSession: () => void;
  onAbandonSession: () => void;
}) {
  return (
    <section className="panel" aria-label="Сессия разговора">
      <div className="panel-header">
        <h2>Сессия</h2>
        {isBusy ? <span className="inline-status">запрос</span> : null}
      </div>
      <dl className="metric-grid">
        <Metric label="ID сессии" value={session?.sessionId ?? "нет"} />
        <Metric label="Статус" value={translateSessionStatus(session?.status)} />
        <Metric label="Версия" value={String(session?.version ?? 0)} />
      </dl>
      {notice ? <p className="notice">{notice}</p> : null}
      <div className="button-row">
        <button type="button" onClick={onCreateSession}>
          Создать сессию
        </button>
        <button type="button" onClick={onRefreshSession} disabled={!session}>
          Обновить сессию
        </button>
        <button type="button" className="danger-button" onClick={onAbandonSession} disabled={!session}>
          Отменить сессию
        </button>
      </div>
    </section>
  );
}

function ProblemPanel({ problem }: { problem: ProblemDetails }) {
  return (
    <section className="panel problem-panel" aria-label="HTTP error">
      <strong>{problem.status}</strong>
      <span>{problem.title}</span>
      {problem.detail ? <p>{problem.detail}</p> : null}
    </section>
  );
}

function RegistrationStatePanel({
  state,
  fieldMap
}: {
  state: RegistrationStateSnapshot | null;
  fieldMap: Map<string, RegistrationFieldSnapshot>;
}) {
  return (
    <section className="panel state-panel" aria-labelledby="state-title">
      <div className="panel-header">
        <h2 id="state-title">Состояние регистрации</h2>
        <span className={state?.registrationCanBeCompleted ? "inline-status inline-status--good" : "inline-status"}>
          {state?.registrationCanBeCompleted ? "готово к завершению" : "в работе"}
        </span>
      </div>

      <div className="field-table" role="table" aria-label="Поля регистрации">
        <div className="field-table__head" role="row">
          <span>Поле</span>
          <span>Статус</span>
          <span>Значение</span>
          <span>Справочник</span>
        </div>
        {orderedFieldNames.map((name) => {
          const field = fieldMap.get(name);
          return (
            <div className="field-table__row" role="row" key={name}>
              <span>{fieldLabels[name] ?? name}</span>
              <StatusBadge status={field?.status ?? "Missing"} />
              <span>{formatValue(field?.value)}</span>
              <span>{field?.referenceId ?? field?.clarificationReason ?? ""}</span>
            </div>
          );
        })}
      </div>

      <IssueList title="Не заполнено" values={state?.missingRequiredFields ?? []} />
      <IssueList title="Ожидает подтверждения" values={state?.fieldsAwaitingConfirmation ?? []} />
      <IssueList title="Требует уточнения" values={state?.fieldsRequiringClarification ?? []} />
      <IssueList title="Проблемы завершения" values={(state?.completionIssues ?? []).map((issue) => `${issue.field}: ${issue.code}`)} />
    </section>
  );
}

function ToolFeedbackPanel({ result }: { result: RegistrationToolResult | null }) {
  const errors = result?.errors ?? [];
  return (
    <section className="panel" aria-labelledby="tool-errors-title">
      <div className="panel-header">
        <h2 id="tool-errors-title">Ошибки инструментов</h2>
        <span className={result?.succeeded ? "inline-status inline-status--good" : "inline-status"}>
          {result ? (result.succeeded ? "ok" : "ошибки") : "ожидание"}
        </span>
      </div>
      {errors.length === 0 ? (
        <p className="state-message">Нет ошибок</p>
      ) : (
        <ul className="error-list">
          {errors.map((error) => (
            <li key={`${error.code}-${error.field ?? "global"}`}>
              <strong>{error.code}</strong>
              <span>{error.message}</span>
              {error.field ? <em>{error.field}</em> : null}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function CompletionPanel({ result }: { result: RegistrationToolResult | null }) {
  const completion = result?.completion;
  return (
    <section className="panel" aria-labelledby="completion-title">
      <div className="panel-header">
        <h2 id="completion-title">Результат</h2>
      </div>
      {completion ? (
        <dl className="result-grid">
          <Metric label="ID регистрации" value={completion.registrationResult.registrationId} tone="good" />
          <Metric label="Завершено" value={formatDate(completion.registrationResult.completedAt)} />
          <Metric label="Имя" value={`${completion.finalRegistration.firstName} ${completion.finalRegistration.lastName}`} />
          <Metric label="Регион" value={completion.finalRegistration.currentRegion} />
        </dl>
      ) : (
        <p className="state-message">Нет результата</p>
      )}
    </section>
  );
}

function TextInput({
  label,
  value,
  onChange,
  type = "text"
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
}) {
  return (
    <label className="field-label">
      <span>{label}</span>
      <input type={type} value={value} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}

function SelectInput({
  label,
  value,
  options,
  onChange
}: {
  label: string;
  value: string;
  options: Array<{ value: string; label: string }>;
  onChange: (value: string) => void;
}) {
  return (
    <label className="field-label">
      <span>{label}</span>
      <select value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">Не выбрано</option>
        {options.map((option) => (
          <option key={`${label}-${option.value}`} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}

function Metric({
  label,
  value,
  tone
}: {
  label: string;
  value: string;
  tone?: "good";
}) {
  return (
    <div>
      <dt>{label}</dt>
      <dd className={tone === "good" ? "metric-good" : undefined}>{value}</dd>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  return <span className={`status-badge status-badge--${status.toLowerCase()}`}>{translateFieldStatus(status)}</span>;
}

function IssueList({ title, values }: { title: string; values: string[] }) {
  return (
    <div className="issue-block">
      <h3>{title}</h3>
      {values.length === 0 ? (
        <span className="muted">нет</span>
      ) : (
        <div className="token-row">
          {values.map((value) => (
            <span className="token" key={value}>{fieldLabels[value] ?? value}</span>
          ))}
        </div>
      )}
    </div>
  );
}

function buildFieldUpdates(form: FormState) {
  const updates: Array<{ name: string; value: unknown }> = [];
  const textFields: Array<[keyof FormState, string]> = [
    ["firstName", "firstName"],
    ["lastName", "lastName"],
    ["patronymic", "patronymic"],
    ["dateOfBirth", "dateOfBirth"],
    ["phoneNumber", "phoneNumber"],
    ["email", "email"],
    ["currentRegion", "currentRegion"],
    ["currentCity", "currentCity"],
    ["actualAddress", "actualAddress"],
    ["userCategory", "userCategory"],
    ["regionBeforeWar", "regionBeforeWar"]
  ];

  for (const [key, name] of textFields) {
    const value = form[key];
    if (typeof value === "string" && value.trim()) {
      updates.push({ name, value: value.trim() });
    }
  }

  if (form.displacedCertificateYear.trim()) {
    updates.push({
      name: "displacedCertificateYear",
      value: Number(form.displacedCertificateYear)
    });
  }

  return updates;
}

function buildConfirmFieldNames(
  form: FormState,
  state: RegistrationStateSnapshot | null
) {
  const fromForm = buildFieldUpdates(form).map((update) => update.name);
  const fromState = (state?.fields ?? [])
    .filter((field) => field.status === "Captured")
    .map((field) => field.name);

  return Array.from(new Set([...fromForm, ...fromState]));
}

function parseFieldNames(value: string): string[] {
  return value
    .split(",")
    .map((part) => part.trim())
    .filter(Boolean);
}

function emptyToUndefined(value: string): string | undefined {
  return value.trim() ? value.trim() : undefined;
}

function formatValue(value: unknown): string {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  if (typeof value === "boolean") {
    return value ? "да" : "нет";
  }

  return String(value);
}

function translateHealthStatus(status: string): string {
  return status === "healthy" ? "здоров" : status;
}

function translateSessionStatus(status: string | undefined): string {
  switch (status) {
    case "Created":
      return "создана";
    case "Connecting":
      return "подключается";
    case "Active":
      return "активна";
    case "Completing":
      return "завершается";
    case "Completed":
      return "завершена";
    case "Failed":
      return "ошибка";
    case "Abandoned":
      return "отменена";
    default:
      return status ?? "нет";
  }
}

function translateFieldStatus(status: string): string {
  switch (status) {
    case "Missing":
      return "не заполнено";
    case "Captured":
      return "получено";
    case "Confirmed":
      return "подтверждено";
    case "NeedsClarification":
      return "нужно уточнить";
    case "Rejected":
      return "отклонено";
    default:
      return status;
  }
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat("ru", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function errorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }

  if (typeof error === "object" && error && "title" in error) {
    return String((error as ProblemDetails).title);
  }

  return "Неизвестная ошибка.";
}

function toProblem(error: unknown, fallbackTitle: string): ProblemDetails {
  if (typeof error === "object" && error && "status" in error && "title" in error) {
    return error as ProblemDetails;
  }

  return {
    title: fallbackTitle,
    status: 0,
    detail: errorMessage(error)
  };
}
