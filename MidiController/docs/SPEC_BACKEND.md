# Backend-Spezifikation – MidiController

## 1. Überblick

Das Backend ist ein **ASP.NET Core Worker Service (.NET 10)**, der gleichzeitig als Windows-Dienst betrieben werden kann. Es übernimmt:

- Empfang von MIDI-Events von physikalischen Geräten
- Verwaltung virtueller MIDI-Ports
- Auswertung von Mapping-Regeln
- Injektion von Eingabe-Events (Tastatur, Maus) in das Betriebssystem
- Bereitstellung einer REST-API und eines WebSocket-Streams für das Frontend

---

## 2. Dienste & Komponenten

### 2.1 MidiInputService (`BackgroundService`)

| Eigenschaft | Beschreibung |
|---|---|
| Bibliothek | NAudio (`MidiIn`) oder RtMidi.NET |
| Threads | Ein Hintergrund-Thread pro physikalischem Gerät |
| Ausgabe | Schreibt `MidiEvent`-Objekte in `System.Threading.Channels.Channel<MidiEvent>` |
| Priorität | `ThreadPriority.Highest` |

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

- Erzeugt logische (virtuelle) MIDI-Ports via **loopMIDI** (COM-Automation) oder **Windows MIDI Services API** (ab Windows 11 24H2).
- Ein physikalisches Gerät kann auf 1–N virtuelle Ports gespiegelt werden.
- Konfiguration in `profiles/{profile}.json` unter `virtualPorts[]`.

### 2.3 EventPipeline

```
MidiInputService
	  │
	  ▼  Channel<MidiEvent>  (bounded, capacity=512)
 MappingWorker  ──────────────────────────────────────────────────▶ InputInjector
	  │
	  └──── LogBroadcastChannel ──▶ SignalR Hub ──▶ Frontend
```

- Verwendet `System.Threading.Channels` (kein Locking, kein Alloc im Hot-Path).
- Der `MappingWorker` läuft auf einem dedizierten Thread mit erhöhter Priorität.
- Das Logging erfolgt auf einem **separaten** Channel (niedrigere Priorität) um den Hot-Path nicht zu blockieren.

### 2.4 MappingEngine

#### 2.4.1 Status-Variablen

Alle persistenten Variablen (**A–Z**) haben den Wertebereich **-127 … +127**. MIDI-Werte Data1/Data2 liegen zwar nur im Bereich 0–127, Differenzwerte (delta) sowie direkte Zuweisungen können aber negativ sein.

| Variable | Alias | Initialwert | Reserviert | Beschreibung |
|---|---|---|---|---|
| `A` | `ActiveListen` | **2** | **Ja** | **Aktivierungs-Gate** (siehe 2.4.2) |
| `B` – `U` | – | 0 | Nein | Frei verwendbare Zustands-Variablen |
| `V` | `DeltaData1` | 0 | **Ja** | Differenz von Data1 zum letzten Event desselben Typs/Kanals |
| `W` | `DeltaData2` | 0 | **Ja** | Differenz von Data2 zum letzten Event desselben Typs/Kanals |
| `X` | `Repeat` | 1 | Nein | Anzahl Tastenwiederholungen (Default: 1) |
| `Y` | `KeyDuration` | 0 | Nein | Dauer des Tastendrucks in ms (Default: 0 = kurzer Tap) |
| `Z` | `Pause` | 0 | Nein | Pause nach dem Tastendruck in ms (Default: 0) |

Zusätzlich stellt das Backend nach jedem Event folgende **berechnete Lesewerte** bereit (schreibgeschützt, nutzbar als Quelle für Bedingungen und X/Y/Z-Zuweisungen):

| Bezeichner | Wertebereich | Beschreibung |
|---|---|---|
| `DD1PosAbs` | 0 – 127 | Absoluter Betrag von DeltaData1, wenn Δ > 0; sonst 0 |
| `DD1NegAbs` | 0 – 127 | Absoluter Betrag von DeltaData1, wenn Δ < 0; sonst 0 |
| `DD2PosAbs` | 0 – 127 | Absoluter Betrag von DeltaData2, wenn Δ > 0; sonst 0 |
| `DD2NegAbs` | 0 – 127 | Absoluter Betrag von DeltaData2, wenn Δ < 0; sonst 0 |
| `DD1Pos` | 0 oder 1 | 1 wenn DeltaData1 > 0, sonst 0 |
| `DD1Neg` | 0 oder 1 | 1 wenn DeltaData1 < 0, sonst 0 |
| `DD2Pos` | 0 oder 1 | 1 wenn DeltaData2 > 0, sonst 0 |
| `DD2Neg` | 0 oder 1 | 1 wenn DeltaData2 < 0, sonst 0 |

> **Regel:** Ist der Delta-Wert 0, sind alle zugehörigen Ableitungen (`*PosAbs`, `*NegAbs`, `*Pos`, `*Neg`) ebenfalls 0. Die Abs-Werte sind immer ≥ 0 (kein Vorzeichen). Dadurch kann man positive und negative Encoder-Bewegungen ohne ELSE-Zweig abbilden, indem zwei Aktionen je mit `DD1PosAbs`/`DD1NegAbs` als X/Y/Z-Quelle konfiguriert werden – eine feuert nur bei positiver, die andere nur bei negativer Drehrichtung.

##### Variable A – Aktivierungs-Gate

| Wert von A | Verhalten |
|---|---|
| **0** | Normal: Alle Trigger werden vollständig ausgewertet. |
| **1** | Pausiert: Prüfblöcke werden übersprungen. Nur Aktionen, die `A` auf `0` setzen, werden ausgeführt. Dient als Soft-Pause / Schutzschalter. |
| **2** (Default) | Gesperrt: MIDI-Events werden gar nicht verarbeitet. Rückkehr zu `A=0` nur durch API-Aufruf oder Frontend möglich. |

> **Hinweis:** `A` darf in Prüfbedingungen gelesen, aber **nicht** als freie Variable für andere Zwecke genutzt werden. In `stateAssignments` ist eine Zuweisung an `A` explizit erlaubt (um den Schalter umzulegen).

##### Variablen V und W – Delta-Werte

Das Backend hält pro MIDI-Device + EventType + Channel + Data1 den zuletzt gesehenen `Data2`-Wert (bzw. `Data1`-Wert für V) in einem Dictionary. Nach jedem eingehenden Event werden V und W **vor** der Triggerauswertung aktualisiert:

```
V = Data1_aktuell - Data1_vorheriges  (0 beim ersten Event)
W = Data2_aktuell - Data2_vorheriges  (0 beim ersten Event)
```

V und W können in Bedingungen und als Quelle für X/Y/Z-Zuweisungen genutzt werden (Quellname `DeltaData1` / `DeltaData2`).

#### 2.4.2 Trigger-Auswertung (pro eingehendem MidiEvent)

```
MIDI-Event eingehend
	   │
	   ▼
┌──────────────────────────────────────────┐
│ Gate-Check: Variable A                   │
│  A==2 → Event verwerfen (return)         │
│  A==1 → nur Aktionen prüfen, die A setzen│
│  A==0 → weiter                          │
└──────────────────┬───────────────────────┘
				   │
				   ▼
┌──────────────────────────────────────────┐
│ Schritt 0: Globale Stati setzen          │  ← optional, wird VOR Prüfblöcken ausgeführt
│   z.B. B=1 ("läuft gerade")             │
└──────────────────┬───────────────────────┘
				   │
				   ▼
┌──────────────────────────────────────────┐  ┐
│ Prüfblock 1                              │  │
│   OR-Bedingungen: mind. eine wahr        │  │
│   z.B. [B==1 OR C>5]                     │  │
├──────────────────┬───────────────────────┤  │ Blöcke sind
│ wahr             │ falsch                │  │ UND-verknüpft
│      ▼           │     ▼ ELSE-Config?    │  │
│ Prüfblock 2      │     ja → ELSE-Zweig   │  │
│   ...            │     nein → return     │  │
└──────────────────┴───────────────────────┘  ┘
	   │ (alle Blöcke wahr)
	   ▼
┌──────────────────────────────────────────┐
│ Aktionen (1..n, sequenziell)             │
│  je Aktion:                              │
│   1. X/Y/Z Quellen auflösen             │
│   2. Tastendruck senden (falls gesetzt)  │
│   3. State-Zuweisungen der Aktion        │
│  nach allen Aktionen:                    │
│   4. Globale Post-Zuweisungen            │
└──────────────────────────────────────────┘
```

**Auswertungsregeln:**
- Prüfblöcke werden sequenziell ausgewertet; alle müssen `true` ergeben (**UND**-Verkettung zwischen Blöcken).
- Innerhalb eines Blocks sind die Bedingungen **ODER**-verknüpft (mindestens eine muss wahr sein).
- Schlägt ein Block fehl und ist eine **ELSE-Config** hinterlegt, wird diese vollständig ausgewertet (eigene Prüfblöcke + Aktionsblock). Die ELSE-Config hat dieselbe Struktur wie ein Trigger.
- Schlägt ein Block fehl ohne ELSE-Config, wird die Verarbeitung dieses Triggers abgebrochen.
- **Globale Pre-Zuweisungen (Schritt 0)** werden immer ausgeführt, bevor Prüfblöcke starten. Damit können laufende Aktionen signalisiert oder andere Trigger geblockt werden.
- **Aktionen** werden sequenziell ausgeführt (Array `actions[]`). Jede Aktion kann eine eigene Tastenkombination, eigene X/Y/Z-Quellen und eigene State-Zuweisungen haben. Aktionen ohne `keyCombination` führen nur State-Änderungen durch. **Globale Post-Zuweisungen** (`globalPostAssignments`) laufen einmalig nach allen Aktionen.

Bedingungen können auf folgende Quellen zugreifen:
- Status-Variablen `A`–`Z` (Wertebereich -127…+127)
- Felder des aktuellen `MidiEvent`: `Data1`, `Data2`, `Channel`, `Type`
- Differenzwerte: `DeltaData1` (= V), `DeltaData2` (= W)

#### 2.4.3 Datenmodelle

```csharp
// Wertebereich aller persistenten Variablen
public const int VarMin = -127;
public const int VarMax = 127;

// Quellen für X/Y/Z-Zuweisungen und StateAssignment-Werte
public enum ValueSource
{
	Fixed,         // harter Wert
	MidiData1,
	MidiData2,
	DeltaData1,    // V: Differenz Data1 zum Vorgänger (-127…+127)
	DeltaData2,    // W: Differenz Data2 zum Vorgänger (-127…+127)
	DD1PosAbs,     // |ΔData1| wenn Δ>0, sonst 0  (0…127)
	DD1NegAbs,     // |ΔData1| wenn Δ<0, sonst 0  (0…127)
	DD2PosAbs,     // |ΔData2| wenn Δ>0, sonst 0  (0…127)
	DD2NegAbs,     // |ΔData2| wenn Δ<0, sonst 0  (0…127)
	DD1Pos,        // 1 wenn ΔData1>0, sonst 0
	DD1Neg,        // 1 wenn ΔData1<0, sonst 0
	DD2Pos,        // 1 wenn ΔData2>0, sonst 0
	DD2Neg,        // 1 wenn ΔData2<0, sonst 0
	VariableA, VariableB, VariableC, VariableD, VariableE,
	VariableF, VariableG, VariableH, VariableI, VariableJ,
	VariableK, VariableL, VariableM, VariableN, VariableO,
	VariableP, VariableQ, VariableR, VariableS, VariableT,
	VariableU, VariableV, VariableW, VariableX, VariableY,
	VariableZ
}

// Eine einzelne Bedingung innerhalb eines Prüfblocks
public record Condition(
	ValueSource Left,      // Variable oder MIDI-Feld
	string      Op,        // "==", "!=", "<", ">", "<=", ">="  
	ValueSource RightSource,
	int         RightFixed
);

// Ein Prüfblock: OR-Verknüpfung seiner Conditions
public record ConditionBlock(
	string?    TemplateName,    // null = inline, sonst Verweis auf gespeichertes Template
	Condition[] Conditions      // mindestens eine muss wahr sein (OR)
);

// Eine einzelne Aktion (Tastendruck + State-Änderungen)
public record ActionBlock(
	string?           TemplateName,
	string[]          KeyCombination,     // leer = nur State-Änderung, kein Tastendruck
	ValueSource       XSource, int XFixed, // Repeat
	ValueSource       YSource, int YFixed, // KeyDuration ms
	ValueSource       ZSource, int ZFixed, // Pause ms
	StateAssignment[] StateAssignments     // Zuweisungen nach dem Tastendruck
);

public record StateAssignment(char Variable, ValueSource Source, int FixedValue);

// Vollständiger Trigger
public record Trigger(
	string           TriggerId,
	string           DeviceId,
	MidiEventType    EventType,
	int              Channel,
	int?             Data1Filter,          // null = beliebig
	StateAssignment[] GlobalPreAssignments, // Schritt 0: immer vor Prüfblöcken
	ConditionBlock[] ConditionBlocks,       // 1..n, UND-verknüpft
	ActionBlock[]    Actions,               // 1..n Aktionen, sequenziell ausgeführt
	StateAssignment[] GlobalPostAssignments,// immer nach allen Aktionen
	TriggerConfig?   ElseConfig             // optional
);

// ELSE-Zweig
public record TriggerConfig(
	ConditionBlock[] ConditionBlocks,
	ActionBlock[]    Actions,
	StateAssignment[] GlobalPostAssignments
);
```

#### 2.4.4 Templates

Prüfblöcke und Aktionsblöcke können unter einem Namen gespeichert und in mehreren Triggern wiederverwendet werden.

Speicherort: `%ProgramData%\MidiController\templates\`

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

REST-API für Templates (unter `/api/v1/templates`):

| Methode | Pfad | Beschreibung |
|---|---|---|
| `GET` | `/templates` | Alle Templates auflisten |
| `GET` | `/templates/{name}` | Template laden |
| `POST` | `/templates` | Template anlegen |
| `PUT` | `/templates/{name}` | Template überschreiben |
| `DELETE` | `/templates/{name}` | Template löschen |

### 2.5 InputInjector

- Implementiert via **Win32 `SendInput`** (P/Invoke).
- Unterstützt: Einzel-Taste, Tastenkombinationen, Key-Down+Hold+Up-Sequenzen.
- `X` = Wiederholungen, `Y` = Hold-Dauer (ms), `Z` = Pause danach (ms).
- Thread-Priorität: `Highest`; kein await auf dem Injection-Pfad.

### 2.6 Config Store

Speicherort: `%ProgramData%\MidiController\profiles\`

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
	  // Schritt 0: immer ausgeführt – B (Sperr-Flag) auf 1 setzen
	  "globalPreAssignments": [
		{ "variable": "B", "source": "Fixed", "fixedValue": 1 }
	  ],
	  // 1..n Prüfblöcke (UND-verknüpft)
	  "conditionBlocks": [
		{ "templateName": "only-when-active" },
		{
		  "conditions": [
			{ "left": "MidiData2", "op": ">=", "rightFixed": 10 },
			{ "left": "VariableC",  "op": "==", "rightFixed": 0 }
		  ]
		}
	  ],
	  // Mehrere Aktionen sequenziell:
	  // Aktion 1: positive Encoder-Drehung → VolumeUp (X = absoluter Positivdelta)
	  // Aktion 2: negative Encoder-Drehung → VolumeDown (X = absoluter Negativdelta)
	  // Wenn der Delta 0 ist, ist jeweils X=0 und SendInput sendet 0 Wiederholungen → kein Effekt
	  "actions": [
		{
		  "keyCombination": ["VolumeUp"],
		  "xSource": "DD1PosAbs", "xFixed": 1,
		  "ySource": "Fixed",     "yFixed": 0,
		  "zSource": "Fixed",     "zFixed": 0,
		  "stateAssignments": []
		},
		{
		  "keyCombination": ["VolumeDown"],
		  "xSource": "DD1NegAbs", "xFixed": 1,
		  "ySource": "Fixed",     "yFixed": 0,
		  "zSource": "Fixed",     "zFixed": 0,
		  "stateAssignments": []
		}
	  ],
	  // Globale Post-Zuweisungen: B zurücksetzen
	  "globalPostAssignments": [
		{ "variable": "B", "source": "Fixed", "fixedValue": 0 }
	  ],
	  // ELSE: wenn ein Prüfblock fehlschlägt
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

## 3. REST-API

Basis-URL: `http://localhost:5173/api/v1`

### 3.1 Devices

| Methode | Pfad | Beschreibung |
|---|---|---|
| `GET` | `/devices` | Alle physikalischen MIDI-Geräte auflisten |
| `GET` | `/devices/virtual` | Alle virtuellen Ports auflisten |
| `POST` | `/devices/virtual` | Virtuellen Port anlegen |
| `DELETE` | `/devices/virtual/{id}` | Virtuellen Port löschen |

### 3.2 Profile

| Methode | Pfad | Beschreibung |
|---|---|---|
| `GET` | `/profiles` | Alle Profile auflisten |
| `GET` | `/profiles/{id}` | Profil laden |
| `POST` | `/profiles` | Profil anlegen |
| `PUT` | `/profiles/{id}` | Profil speichern |
| `DELETE` | `/profiles/{id}` | Profil löschen |
| `POST` | `/profiles/{id}/activate` | Profil aktivieren |

### 3.3 Triggers

| Methode | Pfad | Beschreibung |
|---|---|---|
| `GET` | `/profiles/{id}/triggers` | Trigger eines Profils auflisten |
| `POST` | `/profiles/{id}/triggers` | Trigger anlegen |
| `PUT` | `/profiles/{id}/triggers/{tid}` | Trigger ändern |
| `DELETE` | `/profiles/{id}/triggers/{tid}` | Trigger löschen |

### 3.4 Status

| Methode | Pfad | Beschreibung |
|---|---|---|
| `GET` | `/status` | Aktives Profil, Verbindungsstatus, CPU |
| `GET` | `/status/variables` | Aktuelle Werte A–Z (inkl. X,Y,Z) |
| `PUT` | `/status/variables/{variable}` | Einzelne Variable setzen (Wertebereich -127…+127); insbesondere `A` zum Aktivieren/Deaktivieren der Verarbeitung |

> `PUT /status/variables/A` mit Body `{ "value": 0 }` aktiviert die Verarbeitung. `{ "value": 2 }` sperrt sie. Diese Route ist die einzige Möglichkeit, `A` aus dem Frontend zu steuern, wenn `A==2` gilt (da dann keine Trigger mehr ausgewertet werden).

### 3.5 WebSocket / SignalR

| Hub | Pfad | Events |
|---|---|---|
| `MidiLogHub` | `/hubs/midilog` | `MidiEventReceived(MidiEvent)` |
| `StatusHub` | `/hubs/status` | `VariableChanged(string variableName, int value)` – Name ist Buchstabe (`"A"`) oder Alias (`"ActiveListen"`), Wert ist -127…+127 |

---

## 4. Windows-Dienst-Betrieb

- `UseWindowsService()` in `Program.cs`.
- Installation: `sc create MidiController binPath=...`
- Vorteil: Autostart, läuft auch ohne angemeldeten Benutzer.
- **Einschränkung**: `SendInput` funktioniert nur in der interaktiven Session; der Dienst muss in Session 1 laufen oder per `WTSGetActiveConsoleSessionId` / `CreateProcessAsUser` injizieren (siehe ADR-002).

---

## 5. Fehlerbehandlung & Logging

- `Microsoft.Extensions.Logging` mit Serilog-Sink (File + Console).
- MIDI-Disconnect wird erkannt und der Port automatisch wiederverbunden.
- Fehlerhafte Trigger-Konfigurationen werden beim Laden validiert und als Warning geloggt, der Rest des Profils bleibt aktiv.
