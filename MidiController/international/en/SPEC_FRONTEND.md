# Frontend Specification – MidiController

## 1. Overview

The frontend is a **WPF application (.NET 10)** that also hosts the backend in-process (single-EXE model). It communicates with the backend via **REST** (localhost:5000) and receives real-time events via **SignalR WebSockets**. The application stores no configuration of its own – all data is loaded and saved through the backend API.

> **Single-EXE:** The backend Kestrel host is started automatically in-process on launch via `BackendHostService`. No separate backend process is required.

---

## 2. Screens / Views

### 2.1 Main Window

```
┌──────────────────────────────────────────────────────────┐
│  MidiController  [Profile: gaming ▼]  [Activate] [●]   │
├──────────┬───────────────────────────────────────────────┤
│ Nav      │  Content Area                                 │
│          │                                               │
│ Devices  │                                               │
│ Mappings │                                               │
│ Templates│                                               │
│ Log      │                                               │
│ Status   │                                               │
└──────────┴───────────────────────────────────────────────┘
```

- **Profile dropdown** at the top: select and activate a profile.
- **Status indicator** (●): Green = backend connected + profile active (A=0), Yellow = connected/paused (A=1), Orange = connected/locked (A=2), Red = not connected.
- **Navigation** on the left: switch between the five main sections.

---

### 2.2 View: Devices

**Purpose:** List physical MIDI devices and manage virtual ports.

```
┌─ Physical Devices ──────────────────────────────────────┐
│ [Refresh]                                               │
│                                                         │
│  ► KORG nanoKONTROL2   [Open]                          │
│  ► Arturia KeyStep      [Open]                          │
└─────────────────────────────────────────────────────────┘
┌─ Virtual Ports ─────────────────────────────────────────┐
│  [+ New]                                                │
│                                                         │
│  MidiCtrl-A  → KORG nanoKONTROL2  [Remove]             │
│  MidiCtrl-B  → KORG nanoKONTROL2  [Remove]             │
└─────────────────────────────────────────────────────────┘
```

**Actions:**
- `GET /api/v1/devices` → Refresh device list.
- `POST /api/v1/devices/virtual` → Create a new virtual port (dialog: name + source device).
- `DELETE /api/v1/devices/virtual/{id}` → Remove port.

---

### 2.4 View: Templates

**Purpose:** Manage reusable condition block and action block templates.

```
┌─ Templates  [+ New] ────────────────────────────────────┐
│  Filter: ○ All  ○ ConditionBlock  ○ ActionBlock         │
│                                                         │
│  Name                    │ Type           │ Actions     │
│──────────────────────────┼────────────────┼─────────────│
│  only-when-active        │ ConditionBlock │ ✎ 🗑 📋     │
│  block-during-action     │ ActionBlock    │ ✎ 🗑 📋     │
└─────────────────────────────────────────────────────────┘
```

- **Edit (✎):** Opens the respective block editor inline.
- **Delete (🗑):** First checks whether the template is referenced by any triggers; shows a warning if so.
- **Duplicate (📋):** Create a copy under a new name.
- All operations via `GET/POST/PUT/DELETE /api/v1/templates`.

---

### 2.5 View: Log (MIDI Raw Data)

**Purpose:** Live output of raw MIDI data from a device.

```
┌─ Device: [KORG nanoKONTROL2 ▼]  [Start] [Stop] [Clear] ┐
│ Timestamp    │ Type          │ Ch │ Data1 │ Data2        │
│──────────────┼───────────────┼────┼───────┼──────────────│
│ 00:00:01.023 │ ControlChange │  1 │    16 │          64  │
│ 00:00:01.187 │ NoteOn        │  1 │    60 │         100  │
│ ...          │               │    │       │              │
└──────────────────────────────────────────────────────────┘
```

**Implementation:**
- SignalR connection to `/hubs/midilog`.
- Client subscribes to events for the selected device.
- Maximum buffer: 1,000 rows (oldest are discarded).
- Right-click on a row → **"Create trigger for this event"** (opens the trigger editor pre-filled).

---

### 2.6 View: Mappings (Trigger Configuration)

**Purpose:** Manage mapping rules (triggers) for a profile.

#### 2.4.1 Trigger List

```
┌─ Triggers  [+ New] ─────────────────────────────────────┐
│  # │ Device             │ Event       │ Actions     │ Edit │
│────┼────────────────────┼─────────────┼─────────────┼──────│
│  1 │ nanoKONTROL2 Ch1   │ CC #16      │ Vol↑ / Vol↓ │ ✎ 🗑 │
│  2 │ KeyStep Ch1        │ NoteOn #60  │ Space ×3    │ ✎ 🗑 │
└─────────────────────────────────────────────────────────┘
```

#### 2.4.2 Trigger Editor (Dialog / Side Panel)

```
┌─ Edit Trigger ──────────────────────────────────────────┐
│ [▶ Record]  [■ Stop]  (pre-fill from last event)        │
│                                                         │
│ Device:     [KORG nanoKONTROL2 ▼]                       │
│ Event Type: [ControlChange ▼]  Channel: [1]  Data1: [16]│
│ Match:      [Variable ▼]  Value: [0]                    │
│   (Variable = fire on every Data2; otherwise            │
│    Data2 | DeltaData2 | DD2Positive | DD2Negative)      │
│                                                         │
│ ── Step 0: Global Pre-Assignments ───────────────────── │
│ [+ Assignment]                                          │
│   Variable [B▼] ← [Fixed▼] Value [1]             [✕]   │
│                                                         │
│ ── Condition Blocks (AND-linked) ────────────────────── │
│ [+ Block]                                               │
│ Block 1  Template:[only-when-active▼]        [✕ Block] │
│ Block 2  Template:[-- inline --▼]            [✕ Block] │
│   [+ Condition]                                         │
│   Left [MidiData2▼] Op [>=▼] Right[Fixed▼][10]  [✕]   │
│   ELSE (if Block 2 fails): [▼ configure…]              │
│                                                         │
│ ── Actions (sequential) ─────────────────────────────── │
│ [+ Action]                                              │
│                                                         │
│ Action 1  Template:[-- inline --▼]  [↑][↓][✕]          │
│   Keys: [VolumeUp]  [+ Key]                             │
│   X (Repeat):      ● DD2Positive  ○ DD2Negative  ○ Fixed[_] │
│   Y (KeyDuration): ● Fixed [0]                          │
│   Z (Pause):       ● Fixed [0]                          │
│   State Assignments: [+ Add]                            │
│                                                         │
│ Action 2  Template:[-- inline --▼]  [↑][↓][✕]          │
│   Keys: [VolumeDown]  [+ Key]                           │
│   X (Repeat):      ● DD2Negative  ○ DD2Positive  ○ Fixed[_] │
│   Y (KeyDuration): ● Fixed [0]                          │
│   Z (Pause):       ● Fixed [0]                          │
│   State Assignments: [+ Add]                            │
│                                                         │
│ ── Global Post-Assignments (after all actions) ──────── │
│ [+ Assignment]                                          │
│   Variable [B▼] ← [Fixed▼] Value [0]             [✕]   │
│                                                         │
│         [Cancel]  [Save as Template…]  [Save]           │
└─────────────────────────────────────────────────────────┘
```

**Fields:**

| Section | Field | Type | Description |
|---|---|---|---|
| Header | Record | Button | Starts MIDI recording; clicking a row pre-fills Device/Type/Channel/Data1 |
| Header | Device / Event Type / Channel / Data1 | Dropdowns / Spinners | Filter for MIDI event matching |
| Header | Match (field 4) | Dropdown | `Variable` \| `Data2` \| `DeltaData2` \| `DD2Positive` \| `DD2Negative` |
| Header | Value | Spinner | Target value for match (hidden when `Variable`) |
| Step 0 | Global Pre-Assignments | List | Set variables **before** condition blocks |
| Condition Blocks | Block list | Dynamic | 1..n; order via drag & drop |
| Condition Block | Conditions | List | Left / Op / Right; sources: all variables A–Z, MidiData1/2, DeltaData2, DD2Positive, DD2Negative, Fixed |
| Condition Block | ELSE Config | Collapsible section | Own condition blocks + actions on failure |
| Actions | Action list | Dynamic | 1..n; order via ↑/↓ |
| Action | Template | Dropdown | ActionBlock template or inline |
| Action | Keys | Tag editor | Modifier + keys; empty = state change only |
| Action | X (`Repeat`) | Radio + Spinner | MidiData1/2, DeltaData2, DD2Positive, DD2Negative, Var A–Z, Fixed |
| Action | Y (`KeyDuration`) | Radio + Spinner | Same as X |
| Action | Z (`Pause`) | Radio + Spinner | Same as X |
| Action | State Assignments | List | Variable ← source (after key press of this action) |
| Footer | Global Post-Assignments | List | Executed once after all actions |

> Reserved variables are shown with an alias: `A (ActiveListen)`, `X (Repeat)`, `Y (KeyDuration)`, `Z (Pause)`. Computed `DD2*` values are only selectable as a source (read-only), not as a target.

---

### 2.7 View: Status

**Purpose:** Display the backend runtime status and control variable A.

```
┌─ Runtime Status ────────────────────────────────────────┐
│ Backend:   ● Connected  (localhost:5173)                │
│ Profile:   gaming  [since 00:05:23]                     │
│ Latency:   ~0.8 ms (last 100 events)                   │
│                                                         │
│ ── Processing Gate (A / ActiveListen) ──────────────── │
│ A = 2  ■ LOCKED    [Activate (A=0)] [Pause (A=1)]     │
│         ▤ PAUSED    [Activate (A=0)] [Lock (A=2)]      │
│         ▢ ACTIVE    [Pause (A=1)]   [Lock (A=2)]       │
│                                                         │
│ Status Variables (-127…+127):                           │
│  A=2 (ActiveListen)                                     │
│  B=0   C=0   D=0   E=0   F=0   G=0   H=0   I=0         │
│  J=0   K=0   L=0   M=0   N=0   O=0   P=0   Q=0         │
│  R=0   S=0   T=0   U=0   V=0   W=0                     │
│  X=1 (Repeat)  Y=0 (KeyDuration)  Z=0 (Pause)          │
│                                                         │
│ Computed Read Values (last event):                      │
│  DeltaData2=0  DD2Positive=0  DD2Negative=0             │
└─────────────────────────────────────────────────────────┘
```

**Variable A control:**
- Colored status icon: Red (■ A=2), Yellow (▤ A=1), Green (▢ A=0).
- Buttons call `PUT /api/v1/status/variables/A`.
- **When A=2:** only the "Activate" button is enabled.
- Reserved variables are displayed with their alias in parentheses.
- Computed `DD*` values are read-only and updated after each MIDI event.
- All variables are updated live via SignalR (`/hubs/status`, `VariableChanged`).
- Latency value comes from `GET /api/v1/status`.

---

## 2.8 View: Keyboard Test

**Purpose:** Test keyboard input live without MIDI hardware.

```
┌─ Keyboard Test ─────────────────────────────────────────┐
│  [Ctrl] [Alt] [Shift] [Win]  +  [Enter key______]       │
│                                                         │
│  [▶ Send]   Y (Duration ms): [__0]   Z (Pause ms): [__0]│
│                                                         │
│  Last Action:                                           │
│  → Ctrl+Alt+T sent (12:04:55.321)                       │
└─────────────────────────────────────────────────────────┘
```

- Modifier checkboxes (Ctrl, Alt, Shift, Win) + free-text input for the main key (VK name or character)
- `Y` = KeyDuration, `Z` = Pause (ms)
- Direct call to `POST /api/v1/input/test`

---

## 2.9 Tray Icon (System Notification Area)

The **system tray icon** remains active as long as the application is running. The main window can be minimized – the app continues running invisibly in the background.

**Color coding:**

| Color | Meaning |
|---|---|
| 🟢 Green | Backend connected, profile active (A=0) |
| 🟡 Yellow | Backend connected, processing paused (A=1) |
| 🟠 Orange | Backend connected, gate locked (A=2) |
| 🔴 Red | Backend unreachable |

**Blinking:** The icon blinks briefly (200 ms) on **actual MIDI activity** (received event). It does not blink during pure polling or idle.

**Context menu:**
```
┌─ MidiController ────────────────────────────────────────┐
│  ✔ Activate (A=0)                                       │
│  ✔ Pause (A=1)                                          │
│  ✔ Lock (A=2)                                           │
│  ─────────────────────────────────────────────────────  │
│  Show Window                                            │
│  Exit                                                   │
└─────────────────────────────────────────────────────────┘
```

- **Activate/Pause/Lock** call `PUT /api/v1/status/variables/A`.
- **Exit** stops the backend host and closes the application.
- Double-click on the tray icon opens the main window (if minimized).

---

## 3. API Communication

### 3.1 HTTP Client

- `HttpClient` with `BaseAddress = http://localhost:5173`.
- Retry policy (Polly): 3 attempts, exponential backoff.
- Timeout: 5 seconds per request.
- Connection status is checked periodically (every 5 s) via `GET /api/v1/status`.

### 3.2 SignalR

- `HubConnectionBuilder` with automatic reconnect.
- On connection loss: status indicator turns red; reconnect happens in the background.

---

## 4. Frontend Configuration

`appsettings.json` in the app directory:

```json
{
  "Backend": {
    "BaseUrl": "http://localhost:5173",
    "SignalRReconnectIntervalMs": 3000
  },
  "UI": {
    "LogMaxLines": 1000,
    "Theme": "Dark"
  }
}
```

---

## 5. Technology Stack

| Area | Technology |
|---|---|
| UI Framework | WPF (.NET 10) |
| MVVM | CommunityToolkit.Mvvm |
| HTTP Client | `System.Net.Http.HttpClient` + Polly |
| SignalR Client | `Microsoft.AspNetCore.SignalR.Client` |
| JSON | `System.Text.Json` |
| DI | `Microsoft.Extensions.DependencyInjection` |
