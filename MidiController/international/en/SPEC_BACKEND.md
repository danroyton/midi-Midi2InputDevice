# Backend Specification – MidiController

## 1. Overview

The backend is an **ASP.NET Core Worker Service (.NET 10)** that can simultaneously run as a Windows Service. It handles:

- Receiving MIDI events from physical devices
- Managing virtual MIDI ports
- Evaluating mapping rules
- Injecting input events (keyboard, mouse) into the operating system
- Providing a REST API and a WebSocket stream for the frontend

---

## 2. Services & Components

### 2.1 MidiInputService (`BackgroundService`)

| Property | Description |
|---|---|
| Library | NAudio (`MidiIn`) or RtMidi.NET |
| Threads | One background thread per physical device |
| Output | Writes `MidiEvent` objects into `System.Threading.Channels.Channel<MidiEvent>` |
| Priority | `ThreadPriority.Highest` |

**MidiEvent-Datenmodell:**

```csharp
public record MidiEvent(
	string DeviceId,
	MidiEventType Type,   // NoteOn, NoteOff, ControlChange, ProgramChange, …
	int Channel,
	int Data1,            // Note / CC-Nummer
	int Data2,            // Velocity / CC-Wert
	long TimestampUs      // Mikrosekunden seit Prozessstart
);
```

### 2.2 VirtualMidiPortService

- Creates logical (virtual) MIDI ports via **loopMIDI** (COM automation) or the **Windows MIDI Services API** (from Windows 11 24H2).
- A physical device can be mirrored to 1–N virtual ports.
- Configuration in `profiles/{profile}.json` under `virtualPorts[]`.

### 2.3 EventPipeline

```
MidiInputService
	  │
	  ▼  Channel<MidiEvent>  (bounded, capacity=512)
 MappingWorker  ──────────────────────────────────────────────────▶ InputInjector
	  │
	  └──── LogBroadcastChannel ──▶ SignalR Hub ──▶ Frontend
```

- Uses `System.Threading.Channels` (no locking, no alloc in the hot path).
- The `MappingWorker` runs on a dedicated thread with elevated priority.
- Logging is done on a **separate** channel (lower priority) to avoid blocking the hot path.

### 2.4 MappingEngine

#### 2.4.1 Status Variables

All persistent variables (**A–Z**) have a value range of **-127 … +127**. MIDI values Data1/Data2 are limited to 0–127, but delta values and direct assignments can be negative.

| Variable | Alias | Initial Value | Reserved | Description |
|---|---|---|---|---|
| `A` | `ActiveListen` | **2** | **Yes** | **Activation gate** (see 2.4.2) |
| `B` – `U` | – | 0 | No | Freely usable state variables |
| `X` | `Repeat` | 1 | No | Number of key repetitions (default: 1) |
| `Y` | `KeyDuration` | 0 | No | Key hold duration in ms (default: 0 = short tap) |
| `Z` | `Pause` | 0 | No | Pause after keypress in ms (default: 0) |

Additionally, after each event the backend provides the following **computed read-only values** (read-only, usable as a source for conditions and X/Y/Z assignments):

| Identifier | Value Range | Description |
|---|---|---|
| `DeltaData2` | -127 – 127 | Difference of Data2 from the last event with the same Type+Channel+Data1 (0 on first occurrence) |
| `DD2Positive` | 0 – 127 | Absolute value of DeltaData2 when Δ > 0; otherwise 0 |
| `DD2Negative` | 0 – 127 | Absolute value of DeltaData2 when Δ < 0; otherwise 0 |

> **Tracking rule:** For each triple key `(Type, Channel, Data1)` the last seen `Data2` value is stored. If no entry exists on the first occurrence of this key, the current `Data2` value is used as the predecessor → Delta = 0. This prevents unwanted jump values for switches with fixed offset values.

> **Usage:** Two actions with `DD2Positive` and `DD2Negative` as the X source cover positive and negative encoder rotation directions without an ELSE branch – the action with value 0 sends 0 repetitions and has no effect.

##### Variable A – Activation Gate

| Value of A | Behaviour |
|---|---|
| **0** | Normal: All triggers are fully evaluated. |
| **1** | Paused: Condition blocks are skipped. Only actions that set `A` to `0` are executed. Acts as a soft pause / circuit breaker. |
| **2** (Default) | Blocked: MIDI events are not processed at all. Return to `A=0` only via API call or frontend. |

> **Note:** `A` may be read in condition checks, but **must not** be used as a free variable for other purposes. In `stateAssignments` an assignment to `A` is explicitly allowed (to toggle the switch).

##### DeltaData2 – Tracking Mechanism

The backend maintains a HashMap with the last seen `Data2` value per `(Type, Channel, Data1)`. After each incoming event, `DeltaData2` is calculated **before** trigger evaluation:

```
prev = map[(Type, Channel, Data1)]  if present, otherwise Data2_current
DeltaData2 = Data2_current - prev
map[(Type, Channel, Data1)] = Data2_current
```

The fallback `prev = Data2_current` ensures that Delta = 0 when a key appears for the first time. This avoids erroneous jumps for switches or controls with a fixed offset start value.

#### 2.4.2 Trigger Evaluation (per incoming MidiEvent)

```
Incoming MIDI event
	   │
	   ▼
┌──────────────────────────────────────────┐
│ Gate check: Variable A                   │
│  A==2 → discard event (return)           │
│  A==1 → only check actions that set A   │
│  A==0 → continue                        │
└──────────────────┬───────────────────────┘
				   │
				   ▼
┌──────────────────────────────────────────┐
│ Step 0: Set global state                 │  ← optional, executed BEFORE condition blocks
│   e.g. B=1 ("currently running")        │
└──────────────────┬───────────────────────┘
				   │
				   ▼
┌──────────────────────────────────────────┐  ┐
│ Condition block 1                        │  │
│   OR conditions: at least one true       │  │
│   e.g. [B==1 OR C>5]                    │  │
├──────────────────┬───────────────────────┤  │ Blocks are
│ true             │ false                 │  │ AND-linked
│      ▼           │     ▼ ELSE config?    │  │
│ Condition block 2│     yes → ELSE branch │  │
│   ...            │     no  → return      │  │
└──────────────────┴───────────────────────┘  ┘
	   │ (all blocks true)
	   ▼
┌──────────────────────────────────────────┐
│ Actions (1..n, sequential)               │
│  per action:                             │
│   1. Resolve X/Y/Z sources              │
│   2. Send keypress (if set)             │
│   3. State assignments of this action   │
│  after all actions:                      │
│   4. Global post-assignments            │
└──────────────────────────────────────────┘
```

**Evaluation rules:**
- Condition blocks are evaluated sequentially; all must return `true` (**AND** chaining between blocks).
- Within a block the conditions are **OR**-linked (at least one must be true).
- If a block fails and an **ELSE config** is defined, it is evaluated completely (own condition blocks + action block). The ELSE config has the same structure as a trigger.
- If a block fails without an ELSE config, processing of this trigger is aborted.
- **Global pre-assignments (step 0)** are always executed before condition blocks start. This can be used to signal ongoing actions or block other triggers.
- **Actions** are executed sequentially (array `actions[]`). Each action may have its own key combination, own X/Y/Z sources, and own state assignments. Actions without `keyCombination` only perform state changes. **Global post-assignments** (`globalPostAssignments`) run once after all actions.

Conditions can access the following sources:
- State variables `A`–`Z` (value range -127…+127)
- Fields of the current `MidiEvent`: `Data1`, `Data2`, `Channel`, `Type`
- Delta values: `DeltaData1` (= V), `DeltaData2` (= W)

#### 2.4.3 Data Models

```csharp
// Value range of all persistent variables
public const int VarMin = -127;
public const int VarMax = 127;

// Quellen für X/Y/Z-Zuweisungen und StateAssignment-Werte
public enum ValueSource
{
	Fixed,         // hard-coded value
	MidiData1,
	MidiData2,
	DeltaData2,    // Difference of Data2 from predecessor under (Type, Channel, Data1) (-127…+127)
	DD2Positive,   // |ΔData2| when Δ>0, otherwise 0  (0…127)
	DD2Negative,   // |ΔData2| when Δ<0, otherwise 0  (0…127)
	VariableA, VariableB, VariableC, VariableD, VariableE,
	VariableF, VariableG, VariableH, VariableI, VariableJ,
	VariableK, VariableL, VariableM, VariableN, VariableO,
	VariableP, VariableQ, VariableR, VariableS, VariableT,
	VariableU, VariableV, VariableW, VariableX, VariableY,
	VariableZ
}

// A single condition within a condition block
public record Condition(
	ValueSource Left,      // variable or MIDI field
	string      Op,        // "==", "!=", "<", ">", "<=", ">="  
	ValueSource RightSource,
	int         RightFixed
);

// A condition block: OR combination of its conditions
public record ConditionBlock(
	string?    TemplateName,    // null = inline, otherwise reference to stored template
	Condition[] Conditions      // at least one must be true (OR)
);

// A single action (keypress + state changes)
public record ActionBlock(
	string?           TemplateName,
	string[]          KeyCombination,     // empty = state change only, no keypress
	ValueSource       XSource, int XFixed, // Repeat
	ValueSource       YSource, int YFixed, // KeyDuration ms
	ValueSource       ZSource, int ZFixed, // Pause ms
	StateAssignment[] StateAssignments     // assignments after the keypress
);

// Fourth matching parameter
public enum TriggerMatchMode
{
	Variable,    // fires on every Data2 value (no Data2 filter)
	Data2,       // Data2 == MatchValue
	DeltaData2,  // DeltaData2 == MatchValue
	DD2Positive, // |ΔData2| when Δ>0, otherwise 0  == MatchValue
	DD2Negative, // |ΔData2| when Δ<0, otherwise 0  == MatchValue
}

public record StateAssignment(char Variable, ValueSource Source, int FixedValue);

// Complete trigger
public record Trigger(
	string           TriggerId,
	string           DeviceId,
	MidiEventType    EventType,
	int              Channel,
	int?             Data1Filter,          // null = any
	TriggerMatchMode MatchMode,            // fourth matching parameter (Data2 level)
	int              MatchValue,           // target value (ignored when MatchMode.Variable)
	StateAssignment[] GlobalPreAssignments, // step 0: always before condition blocks
	ConditionBlock[] ConditionBlocks,       // 1..n, AND-linked
	ActionBlock[]    Actions,               // 1..n actions, executed sequentially
	StateAssignment[] GlobalPostAssignments,// always after all actions
	TriggerConfig?   ElseConfig             // optional
);

// ELSE branch
public record TriggerConfig(
	ConditionBlock[] ConditionBlocks,
	ActionBlock[]    Actions,
	StateAssignment[] GlobalPostAssignments
);
```

#### 2.4.4 Templates

Condition blocks and action blocks can be saved under a name and reused in multiple triggers.

Storage location: `%ProgramData%\MidiController\templates\`

```jsonc
// conditionblock-template.json
{
  "templateName": "only-when-active",
  "type": "ConditionBlock",
  "conditions": [
	{ "left": "VariableA", "op": "==", "rightFixed": 0 }
  ]
}

// actionblock-template.json
{
  "templateName": "block-during-action",
  "type": "ActionBlock",
  "globalPreAssignments": [{ "variable": "B", "source": "Fixed", "fixedValue": 1 }],
  "keyCombination": [],
  "xSource": "Fixed", "xFixed": 1,
  "ySource": "Fixed", "yFixed": 0,
  "zSource": "Fixed", "zFixed": 0,
  "stateAssignments": [],
  "globalPostAssignments": [{ "variable": "B", "source": "Fixed", "fixedValue": 0 }]
}
```

REST API for templates (under `/api/v1/templates`):

| Method | Path | Description |
|---|---|---|
| `GET` | `/templates` | List all templates |
| `GET` | `/templates/{name}` | Load a template |
| `POST` | `/templates` | Create a template |
| `PUT` | `/templates/{name}` | Overwrite a template |
| `DELETE` | `/templates/{name}` | Delete a template |

### 2.5 InputInjector

- Implemented via **Win32 `SendInput`** (P/Invoke).
- Supports: single key, key combinations, key-down+hold+up sequences.
- `X` = repetitions, `Y` = hold duration (ms), `Z` = pause after (ms).
- Thread priority: `Highest`; no await on the injection path.

### 2.6 Config Store

Storage location: `%ProgramData%\MidiController\profiles\`

```jsonc
// profile.json (Beispiel)
{
  "profileId": "gaming",
  "devices": [
	{
	  "physicalDeviceId": "KORG nanoKONTROL2",
	  "virtualPorts": ["MidiCtrl-A", "MidiCtrl-B"]
	}
  ],
  "triggers": [
	{
	  "triggerId": "t1",
	  "deviceId": "KORG nanoKONTROL2",
	  "eventType": "ControlChange",
	  "channel": 1,
	  "data1Filter": 16,
	  // fourth matching parameter: "Variable" = fires on every Data2 value
	  "matchMode": "Variable",
	  "matchValue": 0,
	  // Step 0: always executed – set B (lock flag) to 1
	  "globalPreAssignments": [
		{ "variable": "B", "source": "Fixed", "fixedValue": 1 }
	  ],
	  // 1..n condition blocks (AND-linked)
	  "conditionBlocks": [
		{ "templateName": "only-when-active" },
		{
		  "conditions": [
			{ "left": "MidiData2", "op": ">=", "rightFixed": 10 },
			{ "left": "VariableC",  "op": "==", "rightFixed": 0 }
		  ]
		}
	  ],
	  // Multiple actions sequentially:
	  // Action 1: positive encoder rotation → VolumeUp (X = absolute positive delta)
	  // Action 2: negative encoder rotation → VolumeDown (X = absolute negative delta)
	  // When delta is 0, X=0 and SendInput sends 0 repetitions → no effect
	  "actions": [
		{
		  "keyCombination": ["VolumeUp"],
		  "xSource": "DD2Positive", "xFixed": 1,
		  "ySource": "Fixed",       "yFixed": 0,
		  "zSource": "Fixed",       "zFixed": 0,
		  "stateAssignments": []
		},
		{
		  "keyCombination": ["VolumeDown"],
		  "xSource": "DD2Negative", "xFixed": 1,
		  "ySource": "Fixed",       "yFixed": 0,
		  "zSource": "Fixed",       "zFixed": 0,
		  "stateAssignments": []
		}
	  ],
	  // Global post-assignments: reset B
	  "globalPostAssignments": [
		{ "variable": "B", "source": "Fixed", "fixedValue": 0 }
	  ],
	  // ELSE: when a condition block fails
	  "elseConfig": {
		"conditionBlocks": [
		  { "conditions": [{ "left": "VariableD", "op": ">", "rightFixed": 0 }] }
		],
		"actions": [
		  {
			"keyCombination": ["Escape"],
			"xSource": "Fixed", "xFixed": 1,
			"ySource": "Fixed", "yFixed": 0,
			"zSource": "Fixed", "zFixed": 0,
			"stateAssignments": []
		  }
		],
		"globalPostAssignments": []
	  }
	}
  ]
}
```

---

## 3. REST API

Base URL: `http://localhost:5173/api/v1`

### 3.1 Devices

| Method | Path | Description |
|---|---|---|
| `GET` | `/devices` | List all physical MIDI devices |
| `GET` | `/devices/virtual` | List all virtual ports |
| `POST` | `/devices/virtual` | Create a virtual port |
| `DELETE` | `/devices/virtual/{id}` | Delete a virtual port |

### 3.2 Profiles

| Method | Path | Description |
|---|---|---|
| `GET` | `/profiles` | List all profiles |
| `GET` | `/profiles/{id}` | Load a profile |
| `POST` | `/profiles` | Create a profile |
| `PUT` | `/profiles/{id}` | Save a profile |
| `DELETE` | `/profiles/{id}` | Delete a profile |
| `POST` | `/profiles/{id}/activate` | Activate a profile |

### 3.3 Triggers

| Method | Path | Description |
|---|---|---|
| `GET` | `/profiles/{id}/triggers` | List triggers of a profile |
| `POST` | `/profiles/{id}/triggers` | Create a trigger |
| `PUT` | `/profiles/{id}/triggers/{tid}` | Update a trigger |
| `DELETE` | `/profiles/{id}/triggers/{tid}` | Delete a trigger |

### 3.4 Status

| Method | Path | Description |
|---|---|---|
| `GET` | `/status` | Active profile, connection status, CPU |
| `GET` | `/status/variables` | Current values A–Z (incl. X, Y, Z) |
| `PUT` | `/status/variables/{variable}` | Set an individual variable (value range -127…+127); in particular `A` to activate/deactivate processing |

> `PUT /status/variables/A` with body `{ "value": 0 }` activates processing. `{ "value": 2 }` blocks it. This route is the only way to control `A` from the frontend when `A==2` (since no triggers are evaluated in that state).

### 3.5 WebSocket / SignalR

| Hub | Path | Events |
|---|---|---|
| `MidiLogHub` | `/hubs/midilog` | `MidiEventReceived(MidiEvent)` |
| `StatusHub` | `/hubs/status` | `VariableChanged(string variableName, int value)` – name is a letter (`"A"`) or alias (`"ActiveListen"`), value is -127…+127 |

---

## 4. Windows Service Operation

- `UseWindowsService()` in `Program.cs`.
- Installation: `sc create MidiController binPath=...`
- Advantage: auto-start, runs even without a logged-in user.
- **Limitation**: `SendInput` only works in the interactive session; the service must run in Session 1 or inject via `WTSGetActiveConsoleSessionId` / `CreateProcessAsUser` (see ADR-002).

---

## 5. Error Handling & Logging

- `Microsoft.Extensions.Logging` with a Serilog sink (file + console).
- MIDI disconnect is detected and the port is automatically reconnected.
- Faulty trigger configurations are validated on load and logged as a warning; the rest of the profile remains active.
