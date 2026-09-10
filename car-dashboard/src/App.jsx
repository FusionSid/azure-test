import { useState, useEffect } from "react";

const API_BASE = "http://localhost:7071/api";
const DEVICE_ID = "cool-car-1";

function App() {
  const [status, setStatus] = useState(null);
  const [error, setError] = useState(null);
  const [scheduleTime, setScheduleTime] = useState("");
  const [loadingAction, setLoadingAction] = useState(false);

  const fetchStatus = async () => {
    try {
      const res = await fetch(`${API_BASE}/battery/${DEVICE_ID}`);
      if (!res.ok) throw new Error("Failed to fetch status");
      const data = await res.json();
      setStatus(data);
      setError(null);
    } catch (err) {
      setError(err.message);
    }
  };

  useEffect(() => {
    fetchStatus();
    const interval = setInterval(fetchStatus, 3000);
    return () => clearInterval(interval);
  }, []);

  const handleStart = async () => {
    setLoadingAction(true);
    await fetch(`${API_BASE}/charging/${DEVICE_ID}/start`, { method: "POST" });
    await fetchStatus();
    setLoadingAction(false);
  };

  const handleStop = async () => {
    setLoadingAction(true);
    await fetch(`${API_BASE}/charging/${DEVICE_ID}/stop`, { method: "POST" });
    await fetchStatus();
    setLoadingAction(false);
  };

  const handleSetSchedule = async () => {
    if (!scheduleTime) return;
    setLoadingAction(true);
    await fetch(`${API_BASE}/schedule/${DEVICE_ID}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ startTime: scheduleTime }),
    });
    await fetchStatus();
    setLoadingAction(false);
  };

  return (
    <div style={styles.page}>
      <div style={styles.card}>

        <h1 style={styles.title}>My Cool Car</h1>

        {error && <p style={styles.error}>Error: {error}</p>}

        {status ? (
          <>
            <div style={styles.batteryWrap}>
              <div style={styles.batteryOutline}>
                <div
                  style={{
                    ...styles.batteryFill,
                    width: `${status.batteryLevel}%`,
                    backgroundColor: status.batteryLevel > 20 ? "#4caf50" : "#e53935",
                  }}
                />
              </div>
              <p style={styles.batteryText}>{status.batteryLevel}%</p>
            </div>

            <p style={styles.statusLine}>
              Status:{" "}
              <strong style={{ color: status.isCharging ? "#4caf50" : "#888" }}>
                {status.isCharging ? "Charging" : "Not Charging"}
              </strong>
            </p>

            {status.scheduledStartTime && (
              <p style={styles.scheduleLine}>
                Scheduled start: <strong>{status.scheduledStartTime}</strong>
              </p>
            )}

            <div style={styles.buttonRow}>
              <button
                style={{ ...styles.button, ...styles.startButton }}
                onClick={handleStart}
                disabled={loadingAction || status.isCharging}
              >
                Start Charging
              </button>
              <button
                style={{ ...styles.button, ...styles.stopButton }}
                onClick={handleStop}
                disabled={loadingAction || !status.isCharging}
              >
                Stop Charging
              </button>
            </div>

            <div style={styles.scheduleSection}>
              <label style={styles.label}>Set charging schedule:</label>
              <div style={styles.scheduleRow}>
                <input
                  type="time"
                  value={scheduleTime}
                  onChange={(e) => setScheduleTime(e.target.value)}
                  style={styles.input}
                />
                <button
                  style={{ ...styles.button, ...styles.scheduleButton }}
                  onClick={handleSetSchedule}
                  disabled={loadingAction || !scheduleTime}
                >
                  Save
                </button>
              </div>
            </div>

            <p style={styles.lastUpdated}>
              Last updated: {status.lastUpdated ? new Date(status.lastUpdated).toLocaleTimeString() : "—"}
            </p>
          </>
        ) : (
          !error && <p>Loading...</p>
        )}
      </div>
    </div>
  );
}

const styles = {
  page: {
    minHeight: "100vh",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    background: "#111",
    fontFamily: "system-ui, sans-serif",
  },
  card: {
    background: "#1e1e1e",
    borderRadius: 16,
    padding: "32px 40px",
    width: 360,
    color: "#fff",
    boxShadow: "0 8px 24px rgba(0,0,0,0.4)",
  },
  title: { textAlign: "center", marginBottom: 24 },
  batteryWrap: { marginBottom: 16 },
  batteryOutline: {
    width: "100%",
    height: 28,
    border: "2px solid #555",
    borderRadius: 6,
    overflow: "hidden",
    background: "#2a2a2a",
  },
  batteryFill: {
    height: "100%",
    transition: "width 0.5s ease",
  },
  batteryText: { textAlign: "center", marginTop: 6, fontSize: 18, fontWeight: 600 },
  statusLine: { textAlign: "center", fontSize: 16, marginBottom: 8 },
  scheduleLine: { textAlign: "center", fontSize: 14, color: "#aaa", marginBottom: 16 },
  buttonRow: { display: "flex", gap: 12, marginBottom: 24 },
  button: {
    flex: 1,
    padding: "10px 0",
    borderRadius: 8,
    border: "none",
    fontSize: 14,
    fontWeight: 600,
    cursor: "pointer",
  },
  startButton: { background: "#4caf50", color: "#fff" },
  stopButton: { background: "#e53935", color: "#fff" },
  scheduleSection: { borderTop: "1px solid #333", paddingTop: 16 },
  label: { fontSize: 13, color: "#aaa", display: "block", marginBottom: 8 },
  scheduleRow: { display: "flex", gap: 8 },
  input: {
    flex: 1,
    padding: "8px 10px",
    borderRadius: 8,
    border: "1px solid #444",
    background: "#2a2a2a",
    color: "#fff",
  },
  scheduleButton: { background: "#2196f3", color: "#fff", flex: "0 0 80px" },
  lastUpdated: { textAlign: "center", fontSize: 12, color: "#666", marginTop: 16 },
  error: { color: "#e53935", textAlign: "center" },
};

export default App;