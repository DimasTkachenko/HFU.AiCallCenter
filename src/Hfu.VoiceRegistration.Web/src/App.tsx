import { useEffect, useState } from "react";
import { fetchHealth, type HealthResponse } from "./api/healthClient";

type HealthState =
  | { kind: "loading" }
  | { kind: "healthy"; health: HealthResponse }
  | { kind: "error"; message: string };

export default function App() {
  const [healthState, setHealthState] = useState<HealthState>({ kind: "loading" });

  useEffect(() => {
    let isActive = true;

    fetchHealth(import.meta.env.VITE_API_BASE_URL)
      .then((health) => {
        if (isActive) {
          setHealthState({ kind: "healthy", health });
        }
      })
      .catch((error: unknown) => {
        if (isActive) {
          const message = error instanceof Error ? error.message : "Backend health check failed.";
          setHealthState({ kind: "error", message });
        }
      });

    return () => {
      isActive = false;
    };
  }, []);

  return (
    <main className="shell">
      <section className="workspace" aria-labelledby="page-title">
        <div className="masthead">
          <div>
            <p className="eyebrow">HFU PoC</p>
            <h1 id="page-title">HFU Voice Registration Demo</h1>
          </div>
          <StatusPill healthState={healthState} />
        </div>

        <section className="status-panel" aria-label="Backend health">
          <div className="panel-header">
            <div>
              <h2>Backend API</h2>
              <p>Hfu.VoiceRegistration.Api</p>
            </div>
          </div>

          <HealthDetails healthState={healthState} />
        </section>
      </section>
    </main>
  );
}

function StatusPill({ healthState }: { healthState: HealthState }) {
  if (healthState.kind === "healthy") {
    return <span className="status-pill status-pill--healthy">healthy</span>;
  }

  if (healthState.kind === "error") {
    return <span className="status-pill status-pill--error">offline</span>;
  }

  return <span className="status-pill">checking</span>;
}

function HealthDetails({ healthState }: { healthState: HealthState }) {
  if (healthState.kind === "loading") {
    return <p className="state-message">Checking backend health...</p>;
  }

  if (healthState.kind === "error") {
    return <p className="state-message state-message--error">{healthState.message}</p>;
  }

  const { health } = healthState;
  const checkedAt = new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "medium"
  }).format(new Date(health.timestampUtc));

  return (
    <dl className="health-grid">
      <div>
        <dt>Status</dt>
        <dd className="health-value health-value--good">{health.status}</dd>
      </div>
      <div>
        <dt>Service</dt>
        <dd>{health.service}</dd>
      </div>
      <div>
        <dt>Version</dt>
        <dd>{health.version ?? "local"}</dd>
      </div>
      <div>
        <dt>Checked</dt>
        <dd>{checkedAt}</dd>
      </div>
    </dl>
  );
}
