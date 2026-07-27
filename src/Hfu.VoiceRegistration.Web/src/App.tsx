import { useEffect, useMemo, useRef, useState } from "react";
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
import { createConversationRealtimeClient, type ConversationRealtimeClient } from "./api/conversationRealtimeClient";
import { createVoiceAssistantClient, getEffectiveProvider, type IVoiceAssistantClient } from "./api/voiceAssistantClient";
import {
  createOpenAIRealtimeToolBridge,
  type OpenAIRealtimeToolActivity,
  type OpenAIRealtimeToolBridge
} from "./api/openAIRealtimeToolBridge";
import type {
  OpenAIRealtimeEventLogEntry,
  OpenAIRealtimeTranscriptEntry,
  OpenAIRealtimeVoiceConnectionState,
  OpenAIRealtimeWebRtcClient
} from "./api/openAIRealtimeTypes";
import type {
  ConversationRealtimeEvent,
  RealtimeConnectionState
} from "./api/realtimeTypes";
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

const startInterviewInstructions =
  "Start the HFU demo registration interview now. Speak Ukrainian. First call get_registration_state, then ask the first needed registration question. Do not wait for the user to speak first.";

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
  const [liveState, setLiveState] = useState<RealtimeConnectionState>({ status: "idle" });
  const [liveEvents, setLiveEvents] = useState<ConversationRealtimeEvent[]>([]);
  const [aiProvider, setAiProvider] = useState<"openai" | "gemini">("openai");
  const [voiceState, setVoiceState] = useState<OpenAIRealtimeVoiceConnectionState>({ status: "idle" });
  const [voiceTranscripts, setVoiceTranscripts] = useState<OpenAIRealtimeTranscriptEntry[]>([]);
  const [voiceEvents, setVoiceEvents] = useState<OpenAIRealtimeEventLogEntry[]>([]);
  const [voiceToolActivities, setVoiceToolActivities] = useState<OpenAIRealtimeToolActivity[]>([]);
  const realtimeClientRef = useRef<ConversationRealtimeClient | null>(null);
  const voiceClientRef = useRef<IVoiceAssistantClient | null>(null);
  const voiceToolBridgeRef = useRef<OpenAIRealtimeToolBridge | null>(null);
  const voiceUnsubscribersRef = useRef<Array<() => void>>([]);
  const currentSessionIdRef = useRef<string | null>(null);

  useEffect(() => {
    const realtimeClient = createConversationRealtimeClient({ baseUrl: apiBaseUrl });
    realtimeClientRef.current = realtimeClient;

    const unsubscribeEvent = realtimeClient.onEvent((conversationEvent) => {
      setLiveEvents((current) => [
        conversationEvent,
        ...current.filter((item) => item.eventId !== conversationEvent.eventId)
      ].slice(0, 8));

      if (conversationEvent.sessionId !== currentSessionIdRef.current) {
        return;
      }

      getConversationSession(conversationEvent.sessionId, apiBaseUrl)
        .then((refreshedSession) => {
          applySession(refreshedSession);
        })
        .catch((error: unknown) => {
          setProblem(toProblem(error, "Не удалось обновить сессию после live события."));
        });
    });
    const unsubscribeStatus = realtimeClient.onStatusChange((state) => {
      setLiveState(state);
      if (state.status === "connected" && currentSessionIdRef.current) {
        realtimeClient.joinSession(currentSessionIdRef.current)
          .catch((error: unknown) => {
            setLiveState({ status: "error", message: errorMessage(error) });
          });
      }
    });

    return () => {
      unsubscribeEvent();
      unsubscribeStatus();
      void realtimeClient.stop();
      realtimeClientRef.current = null;
    };
  }, [apiBaseUrl]);

  useEffect(() => {
    return () => {
      disposeVoiceClient();
    };
  }, []);

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
            const previousSessionId = currentSessionIdRef.current;
            applySession(restoredSession);
            setNotice("Сессия восстановлена");
            void joinLiveSession(restoredSession.sessionId, previousSessionId);
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
    const previousSessionId = currentSessionIdRef.current;
    if (previousSessionId && previousSessionId !== nextSession.sessionId) {
      disposeVoiceClient();
      setVoiceState({ status: "idle" });
      setVoiceTranscripts([]);
      setVoiceEvents([]);
      setVoiceToolActivities([]);
    }

    currentSessionIdRef.current = nextSession.sessionId;
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
      const previousSessionId = currentSessionIdRef.current;
      localStorage.setItem(sessionStorageKey, created.sessionId);
      applySession(created);
      setLastToolResult(null);
      setNotice("Сессия создана");
      await joinLiveSession(created.sessionId, previousSessionId);
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
      stopVoiceClient({ status: "stopped" }, true);
      await leaveLiveSession(currentSessionId);
    });
  }

  async function joinLiveSession(
    nextSessionId: string,
    previousSessionId: string | null
  ) {
    const realtimeClient = realtimeClientRef.current;
    if (!realtimeClient) {
      return;
    }

    try {
      await realtimeClient.connect();
      if (previousSessionId && previousSessionId !== nextSessionId) {
        await realtimeClient.leaveSession(previousSessionId);
      }

      await realtimeClient.joinSession(nextSessionId);
    } catch (error: unknown) {
      setLiveState({ status: "error", message: errorMessage(error) });
    }
  }

  async function leaveLiveSession(sessionId: string) {
    const realtimeClient = realtimeClientRef.current;
    if (!realtimeClient) {
      return;
    }

    try {
      await realtimeClient.leaveSession(sessionId);
    } catch (error: unknown) {
      setLiveState({ status: "error", message: errorMessage(error) });
    }
  }

  async function handleStartVoice() {
    if (!currentSessionId) {
      return;
    }

    disposeVoiceClient();
    setProblem(null);
    setVoiceTranscripts([]);
    setVoiceEvents([]);
    setVoiceToolActivities([]);

    const provider = getEffectiveProvider();
    const voiceClient = createVoiceAssistantClient({
      baseUrl: apiBaseUrl,
      sessionId: currentSessionId,
      provider
    });
    voiceClientRef.current = voiceClient;

    if (provider === "openai") {
      voiceToolBridgeRef.current = createOpenAIRealtimeToolBridge({
        sessionId: currentSessionId,
        baseUrl: apiBaseUrl,
        voiceClient: voiceClient as any,
        onToolResult: applyToolResult,
        onActivity: (activity) => {
          setVoiceToolActivities((current) => upsertToolActivity(current, activity).slice(0, 6));
        }
      });
    }

    const unsubscribers: Array<() => void> = [
      voiceClient.onStateChange(setVoiceState)
    ];

    if (voiceClient.onTranscript) {
      unsubscribers.push(
        voiceClient.onTranscript((entry) => {
          setVoiceTranscripts((current) => upsertTranscriptEntry(current, entry).slice(-12));
        })
      );
    }

    if (voiceClient.onEvent) {
      unsubscribers.push(
        voiceClient.onEvent((event) => {
          setVoiceEvents((current) => [
            event,
            ...current.filter((item) => item.id !== event.id)
          ].slice(0, 6));
        })
      );
    }

    voiceUnsubscribersRef.current = unsubscribers;

    try {
      await voiceClient.start();
      if (voiceClient.sendEvent) {
        voiceClient.sendEvent({
          type: "response.create",
          response: {
            instructions: startInterviewInstructions
          }
        });
      }
    } catch (error: unknown) {
      disposeVoiceClient();
      const problemDetails = toProblem(error, `Не удалось запустить связь (${provider}).`);
      setProblem(problemDetails);
      setVoiceState({
        status: "error",
        message: problemDetails.detail ?? problemDetails.title
      });
    }
  }

  function handleStopVoice() {
    stopVoiceClient({ status: "stopped" });
  }

  function stopVoiceClient(
    nextState: OpenAIRealtimeVoiceConnectionState,
    clearHistory = false
  ) {
    disposeVoiceClient();
    if (clearHistory) {
      setVoiceTranscripts([]);
      setVoiceEvents([]);
      setVoiceToolActivities([]);
    }

    setVoiceState(nextState);
  }

  function disposeVoiceClient() {
    voiceToolBridgeRef.current?.dispose();
    voiceToolBridgeRef.current = null;

    for (const unsubscribe of voiceUnsubscribersRef.current) {
      unsubscribe();
    }

    voiceUnsubscribersRef.current = [];
    voiceClientRef.current?.stop();
    voiceClientRef.current = null;
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
        stopVoiceClient({ status: "stopped" });
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

  const isTerminalSession = session?.status === "Completed" || session?.status === "Abandoned";
  const isVoiceActive = voiceState.status === "requesting_microphone"
    || voiceState.status === "connecting"
    || voiceState.status === "connected";
  const canStartVoice = Boolean(currentSessionId) && !isTerminalSession && !isVoiceActive;
  const canStopVoice = isVoiceActive || voiceClientRef.current !== null;

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
            liveState={liveState}
            onCreateSession={handleCreateSession}
            onRefreshSession={handleRefreshSession}
            onAbandonSession={handleAbandonSession}
          />
          <VoicePanel
            voiceState={voiceState}
            transcripts={voiceTranscripts}
            events={voiceEvents}
            toolActivities={voiceToolActivities}
            canStart={canStartVoice}
            canStop={canStopVoice}
            onStartVoice={handleStartVoice}
            onStopVoice={handleStopVoice}
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
                options={regions.map((region) => ({ value: region.name, label: region.name }))}
              />
              <TextInput
                label="Год справки ВПО"
                value={form.displacedCertificateYear}
                onChange={(value) => updateForm("displacedCertificateYear", value)}
              />
            </div>

            <div className="checkbox-row">
              <label>
                <input
                  type="checkbox"
                  checked={form.personalDataConsent}
                  onChange={(e) => updateForm("personalDataConsent", e.target.checked)}
                />
                Согласие на обработку перс. данных
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={form.registrationConfirmed}
                  onChange={(e) => updateForm("registrationConfirmed", e.target.checked)}
                />
                Окончательное подтверждение регистрации
              </label>
            </div>

            <div className="button-row form-actions">
              <button
                type="button"
                className="primary-button"
                onClick={handleUpdateFields}
                disabled={!currentSessionId || isBusy}
              >
                Сохранить поля
              </button>
              <button
                type="button"
                onClick={handleConfirmFields}
                disabled={!currentSessionId || isBusy}
              >
                Подтвердить поля
              </button>
              <button
                type="button"
                onClick={handleCompleteRegistration}
                disabled={!currentSessionId || isBusy}
              >
                Завершить регистрацию
              </button>
            </div>
          </section>

          <section className="layout-side">
            <LiveEventsPanel events={liveEvents} />
            <RegistrationStatePanel state={registrationState} fieldMap={fieldMap} />
            <ToolFeedbackPanel result={lastToolResult} />
            <CompletionPanel result={lastToolResult} />
          </section>
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
  liveState,
  onCreateSession,
  onRefreshSession,
  onAbandonSession
}: {
  session: ConversationSessionResponse | null;
  notice: string | null;
  isBusy: boolean;
  liveState: RealtimeConnectionState;
  onCreateSession: () => void;
  onRefreshSession: () => void;
  onAbandonSession: () => void;
}) {
  return (
    <section className="panel" aria-label="Сессия разговора">
      <div className="panel-header">
        <h2>Сессия</h2>
        <div className="status-row">
          <span className={liveState.status === "connected" ? "inline-status inline-status--good" : "inline-status"}>
            {translateLiveStatus(liveState.status)}
          </span>
          {isBusy ? <span className="inline-status">запрос</span> : null}
        </div>
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

function VoicePanel({
  voiceState,
  transcripts,
  events,
  toolActivities,
  canStart,
  canStop,
  onStartVoice,
  onStopVoice
}: {
  voiceState: OpenAIRealtimeVoiceConnectionState;
  transcripts: OpenAIRealtimeTranscriptEntry[];
  events: OpenAIRealtimeEventLogEntry[];
  toolActivities: OpenAIRealtimeToolActivity[];
  canStart: boolean;
  canStop: boolean;
  onStartVoice: () => void;
  onStopVoice: () => void;
}) {
  const provider = getEffectiveProvider();
  const providerLabel = provider === "gemini" ? "Gemini Live" : "OpenAI Realtime";

  const statusClassName = voiceState.status === "connected"
    ? "inline-status inline-status--good"
    : voiceState.status === "error"
      ? "inline-status inline-status--error"
      : "inline-status";

  return (
    <section className="panel voice-panel" aria-labelledby="voice-panel-title">
      <div className="panel-header">
        <h2 id="voice-panel-title">Голос <span style={{ fontSize: "0.75rem", opacity: 0.8, fontWeight: "normal" }}>({providerLabel})</span></h2>
        <span className={statusClassName}>{translateVoiceStatus(voiceState.status)}</span>
      </div>

      <div className="voice-content">
        <div className="button-row voice-actions">
          <button type="button" className="primary-button" onClick={onStartVoice} disabled={!canStart}>
            Начать голос
          </button>
          <button type="button" onClick={onStopVoice} disabled={!canStop}>
            Остановить
          </button>
        </div>

        {voiceState.status === "error" && voiceState.message ? (
          <p className="state-message state-message--error voice-message">{voiceState.message}</p>
        ) : null}

        <div className="voice-section">
          <div className="voice-section-header">
            <h3>Транскрипт</h3>
            <span className="inline-status">{transcripts.length}</span>
          </div>
          {transcripts.length === 0 ? (
            <p className="state-message voice-empty">Нет реплик</p>
          ) : (
            <ol className="transcript-list">
              {transcripts.map((entry) => (
                <li key={entry.id}>
                  <strong>{entry.role === "user" ? "Клиент" : "AI"}</strong>
                  <span>{entry.text}</span>
                  {!entry.isFinal ? <em>черновик</em> : null}
                </li>
              ))}
            </ol>
          )}
        </div>

        <div className="voice-section">
          <div className="voice-section-header">
            <h3>Realtime events</h3>
            <span className="inline-status">{events.length}</span>
          </div>
          {events.length === 0 ? (
            <p className="state-message voice-empty">Нет событий</p>
          ) : (
            <ul className="voice-event-list">
              {events.map((event) => (
                <li key={event.id}>{event.type}</li>
              ))}
            </ul>
          )}
        </div>

        <div className="voice-section">
          <div className="voice-section-header">
            <h3>AI tools</h3>
            <span className="inline-status">{toolActivities.length}</span>
          </div>
          {toolActivities.length === 0 ? (
            <p className="state-message voice-empty">Нет вызовов</p>
          ) : (
            <ul className="voice-tool-list">
              {toolActivities.map((activity) => (
                <li key={activity.callId}>
                  <strong>{activity.name}</strong>
                  <span className={`status-badge status-badge--tool-${activity.status}`}>
                    {translateToolActivityStatus(activity.status)}
                  </span>
                  {activity.message ? <em>{activity.message}</em> : null}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </section>
  );
}

function LiveEventsPanel({ events }: { events: ConversationRealtimeEvent[] }) {
  return (
    <section className="panel" aria-labelledby="live-events-title">
      <div className="panel-header">
        <h2 id="live-events-title">Живые события</h2>
        <span className="inline-status">{events.length}</span>
      </div>
      {events.length === 0 ? (
        <p className="state-message">Нет live событий</p>
      ) : (
        <ul className="live-event-list">
          {events.map((conversationEvent) => (
            <li key={conversationEvent.eventId}>
              <strong>{conversationEvent.type}</strong>
              <span>{conversationEvent.message}</span>
              <em>v{conversationEvent.version}</em>
            </li>
          ))}
        </ul>
      )}
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

function translateLiveStatus(status: RealtimeConnectionState["status"]): string {
  switch (status) {
    case "connecting":
      return "подключение";
    case "connected":
      return "live подключено";
    case "reconnecting":
      return "reconnect";
    case "disconnected":
      return "live отключено";
    case "error":
      return "live ошибка";
    default:
      return "live ожидание";
  }
}

function translateVoiceStatus(status: OpenAIRealtimeVoiceConnectionState["status"]): string {
  switch (status) {
    case "requesting_microphone":
      return "доступ к микрофону";
    case "connecting":
      return "голос подключается";
    case "connected":
      return "голос подключён";
    case "stopped":
      return "голос остановлен";
    case "error":
      return "ошибка голоса";
    default:
      return "голос ожидание";
  }
}

function translateToolActivityStatus(status: OpenAIRealtimeToolActivity["status"]): string {
  switch (status) {
    case "running":
      return "в работе";
    case "completed":
      return "готово";
    case "error":
      return "ошибка";
    default:
      return status;
  }
}

function upsertTranscriptEntry(
  entries: OpenAIRealtimeTranscriptEntry[],
  entry: OpenAIRealtimeTranscriptEntry
): OpenAIRealtimeTranscriptEntry[] {
  const existingIndex = entries.findIndex((current) => current.id === entry.id);
  if (existingIndex < 0) {
    return [...entries, entry];
  }

  const updated = [...entries];
  updated[existingIndex] = entry;

  return updated;
}

function upsertToolActivity(
  activities: OpenAIRealtimeToolActivity[],
  activity: OpenAIRealtimeToolActivity
): OpenAIRealtimeToolActivity[] {
  return [
    activity,
    ...activities.filter((current) => current.callId !== activity.callId)
  ];
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
