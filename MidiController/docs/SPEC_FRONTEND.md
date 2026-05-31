# Frontend-Spezifikation – MidiController

## 1. Überblick

Das Frontend ist eine **WPF-Applikation (.NET 10)**, die ausschließlich zur Konfiguration dient. Es kommuniziert mit dem Backend über **REST** und empfängt Echtzeit-Events über **SignalR WebSockets**. Die Applikation speichert keine eigene Konfiguration – alle Daten werden über die Backend-API geladen und gespeichert.

---

## 2. Screens / Views

### 2.1 Hauptfenster

```
┌──────────────────────────────────────────────────────────┐
│  MidiController  [Profil: gaming ▼]  [Aktivieren] [●]   │
├──────────┬───────────────────────────────────────────────┤
│ Nav      │  Content-Area                                 │
│          │                                               │
│ Devices  │                                               │
│ Mappings │                                               │
│ Templates│                                               │
│ Log      │                                               │
│ Status   │                                               │
└──────────┴───────────────────────────────────────────────┘
```

- **Profil-Dropdown** oben: Auswahl und Aktivierung eines Profils.
- **Status-Indikator** (●): Grün = Backend verbunden + Profil aktiv (A=0), Gelb = verbunden/pausiert (A=1), Orange = verbunden/gesperrt (A=2), Rot = nicht verbunden.
- **Navigation** links: Wechsel zwischen den fünf Hauptbereichen.

---

### 2.2 View: Devices

**Zweck:** Physikalische MIDI-Geräte auflisten und virtuelle Ports verwalten.

```
┌─ Physikalische Geräte ──────────────────────────────────┐
│ [Aktualisieren]                                         │
│                                                         │
│  ► KORG nanoKONTROL2   [Öffnen]                        │
│  ► Arturia KeyStep      [Öffnen]                        │
└─────────────────────────────────────────────────────────┘
┌─ Virtuelle Ports ───────────────────────────────────────┐
│  [+ Neu]                                                │
│                                                         │
│  MidiCtrl-A  → KORG nanoKONTROL2  [Entfernen]          │
│  MidiCtrl-B  → KORG nanoKONTROL2  [Entfernen]          │
└─────────────────────────────────────────────────────────┘
```

**Aktionen:**
- `GET /api/v1/devices` → Geräteliste aktualisieren.
- `POST /api/v1/devices/virtual` → Neuen virtuellen Port anlegen (Dialog: Name + Quellgerät).
- `DELETE /api/v1/devices/virtual/{id}` → Port entfernen.

---

### 2.4 View: Templates

**Zweck:** Wiederverwendbare Prüfblock- und Aktionsblock-Templates verwalten.

```
┌─ Templates  [+ Neu] ────────────────────────────────────┐
│  Filter: ○ Alle  ○ ConditionBlock  ○ ActionBlock        │
│                                                         │
│  Name                    │ Typ            │ Aktionen    │
│──────────────────────────┼────────────────┼─────────────│
│  only-when-active        │ ConditionBlock │ ✎ 🗑 📋     │
│  block-during-action     │ ActionBlock    │ ✎ 🗑 📋     │
└─────────────────────────────────────────────────────────┘
```

- **Bearbeiten (✎):** Öffnet den jeweiligen Block-Editor inline.
- **Löschen (🗑):** Prüft zuerst, ob das Template von Triggern referenziert wird; zeigt Warnung wenn ja.
- **Duplizieren (📋):** Kopie unter neuem Namen anlegen.
- Alle Operationen via `GET/POST/PUT/DELETE /api/v1/templates`.

---

### 2.5 View: Log (MIDI Raw Data)

**Zweck:** Live-Ausgabe der MIDI-Rohdaten eines Geräts.

```
┌─ Gerät: [KORG nanoKONTROL2 ▼]  [Start] [Stop] [Leeren] ┐
│ Timestamp    │ Type          │ Ch │ Data1 │ Data2        │
│──────────────┼───────────────┼────┼───────┼──────────────│
│ 00:00:01.023 │ ControlChange │  1 │    16 │          64  │
│ 00:00:01.187 │ NoteOn        │  1 │    60 │         100  │
│ ...          │               │    │       │              │
└──────────────────────────────────────────────────────────┘
```

**Implementierung:**
- SignalR-Verbindung zu `/hubs/midilog`.
- Client abonniert events für das ausgewählte Gerät.
- Maximalpuffer: 1000 Zeilen (älteste werden verworfen).
- Rechtsklick auf eine Zeile → **„Trigger für dieses Event anlegen"** (öffnet Trigger-Editor vorausgefüllt).

---

### 2.6 View: Mappings (Trigger-Konfiguration)

**Zweck:** Mapping-Regeln (Trigger) eines Profils verwalten.

#### 2.4.1 Trigger-Liste

```
┌─ Trigger  [+ Neu] ──────────────────────────────────────┐
│  # │ Gerät              │ Event       │ Aktionen    │ Edit │
│────┼────────────────────┼─────────────┼─────────────┼──────│
│  1 │ nanoKONTROL2 Ch1   │ CC #16      │ Vol↑ / Vol↓ │ ✎ 🗑 │
│  2 │ KeyStep Ch1        │ NoteOn #60  │ Space ×3    │ ✎ 🗑 │
└─────────────────────────────────────────────────────────┘
```

#### 2.4.2 Trigger-Editor (Dialog / Side-Panel)

```
┌─ Trigger bearbeiten ────────────────────────────────────┐
│ [▶ Mitschneiden]  [■ Stop]  (letztes Event vorausfüllen) │
│                                                         │
│ Gerät:      [KORG nanoKONTROL2 ▼]                       │
│ Event-Typ:  [ControlChange ▼]   Kanal: [1]  Data1: [16] │
│ Match:      [Variable ▼]  Wert: [0]                     │
│   (Variable = bei jedem Data2 auslösen; sonst            │
│    Data2 | DeltaData2 | DD2Positive | DD2Negative)      │
│                                                         │
│ ── Schritt 0: Globale Pre-Zuweisungen ───────────────── │
│ [+ Zuweisung]                                           │
│   Variable [B▼] ← [Fixed▼] Wert [1]              [✕]   │
│                                                         │
│ ── Prüfblöcke (UND-verknüpft) ───────────────────────── │
│ [+ Block]                                               │
│ Block 1  Template:[only-when-active▼]        [✕ Block] │
│ Block 2  Template:[-- inline --▼]            [✕ Block] │
│   [+ Bedingung]                                         │
│   Links [MidiData2▼] Op [>=▼] Rechts[Fixed▼][10] [✕]  │
│   ELSE (wenn Block 2 falsch): [▼ konfigurieren…]       │
│                                                         │
│ ── Aktionen (sequenziell) ───────────────────────────── │
│ [+ Aktion]                                              │
│                                                         │
│ Aktion 1  Template:[-- inline --▼]  [↑][↓][✕]          │
│   Tasten: [VolumeUp]  [+ Taste]                         │
│   X (Repeat):      ● DD2Positive  ○ DD2Negative  ○ Fest[_] │
│   Y (KeyDuration): ● Fest [0]                           │
│   Z (Pause):       ● Fest [0]                           │
│   State-Zuweisungen: [+ Hinzufügen]                     │
│                                                         │
│ Aktion 2  Template:[-- inline --▼]  [↑][↓][✕]          │
│   Tasten: [VolumeDown]  [+ Taste]                       │
│   X (Repeat):      ● DD2Negative  ○ DD2Positive  ○ Fest[_] │
│   Y (KeyDuration): ● Fest [0]                           │
│   Z (Pause):       ● Fest [0]                           │
│   State-Zuweisungen: [+ Hinzufügen]                     │
│                                                         │
│ ── Globale Post-Zuweisungen (nach allen Aktionen) ───── │
│ [+ Zuweisung]                                           │
│   Variable [B▼] ← [Fixed▼] Wert [0]              [✕]   │
│                                                         │
│         [Abbrechen]  [Als Template…]  [Speichern]       │
└─────────────────────────────────────────────────────────┘
```

**Felder:**

| Bereich | Feld | Typ | Beschreibung |
|---|---|---|---|
| Header | Mitschneiden | Button | Startet MIDI-Mitschnitt; bei Klick auf eine Zeile werden Gerät/Typ/Kanal/Data1 vorausgefüllt |
| Header | Gerät / Event-Typ / Kanal / Data1 | Dropdowns / Spinner | Filter für MIDI-Event-Matching |
| Header | Match (Feld 4) | Dropdown | `Variable` \| `Data2` \| `DeltaData2` \| `DD2Positive` \| `DD2Negative` |
| Header | Wert | Spinner | Zielwert für Match (ausgeblendet bei `Variable`) |
| Schritt 0 | Globale Pre-Zuweisungen | Liste | Variablen setzen **vor** Prüfblöcken |
| Prüfblöcke | Block-Liste | Dynamisch | 1..n; Reihenfolge per Drag & Drop |
| Prüfblock | Bedingungen | Liste | Links / Op / Rechts; Quellen: alle Variablen A–Z, MidiData1/2, DeltaData2, DD2Positive, DD2Negative, Fixed |
| Prüfblock | ELSE-Config | Ausklappbereich | Eigene Prüfblöcke + Aktionen bei Fehlschlag |
| Aktionen | Aktions-Liste | Dynamisch | 1..n; Reihenfolge per ↑/↓ |
| Aktion | Template | Dropdown | ActionBlock-Template oder inline |
| Aktion | Tasten | Tag-Editor | Modifier + Tasten; leer = nur State-Änderung |
| Aktion | X (`Repeat`) | Radio + Spinner | MidiData1/2, DeltaData2, DD2Positive, DD2Negative, Var A–Z, Fixed |
| Aktion | Y (`KeyDuration`) | Radio + Spinner | Wie X |
| Aktion | Z (`Pause`) | Radio + Spinner | Wie X |
| Aktion | State-Zuweisungen | Liste | Variable ← Quelle (nach dem Tastendruck dieser Aktion) |
| Footer | Globale Post-Zuweisungen | Liste | Einmalig nach allen Aktionen ausgeführt |

> Reservierte Variablen erscheinen mit Alias: `A (ActiveListen)`, `X (Repeat)`, `Y (KeyDuration)`, `Z (Pause)`. Berechnete `DD2*`-Werte sind nur als Quelle (lesend) wählbar, nicht als Ziel.

---

### 2.5 View: Templates

**Zweck:** Wiederverwendbare Prüfblock- und Aktionsblock-Templates verwalten.

```
┌─ Templates  [+ Neu] ────────────────────────────────────┐
│  Filter: ○ Alle  ○ ConditionBlock  ○ ActionBlock        │
│                                                         │
│  Name                    │ Typ            │ Aktionen    │
│──────────────────────────┼────────────────┼─────────────│
│  only-when-active        │ ConditionBlock │ ✎ 🗑 📋     │
│  block-during-action     │ ActionBlock    │ ✎ 🗑 📋     │
└─────────────────────────────────────────────────────────┘
```

- **Bearbeiten (✎):** Öffnet den jeweiligen Block-Editor inline.
- **Löschen (🗑):** Prüft zuerst, ob das Template von Triggern referenziert wird; zeigt Warnung wenn ja.
- **Duplizieren (📋):** Kopie unter neuem Namen anlegen.
- Alle Operationen via `GET/POST/PUT/DELETE /api/v1/templates`.

---

### 2.7 View: Status

**Zweck:** Laufzeitstatus des Backends anzeigen und Variable A steuern.

```
┌─ Laufzeit-Status ───────────────────────────────────────┐
│ Backend:   ● Verbunden  (localhost:5173)                 │
│ Profil:    gaming  [seit 00:05:23]                       │
│ Latenz:    ~0.8 ms (letzte 100 Events)                  │
│                                                         │
│ ── Verarbeitungs-Gate (A / ActiveListen) ────────────── │
│ A = 2  ■ GESPERRT   [Aktivieren (A=0)] [Pause (A=1)]  │
│         ▤ PAUSIERT   [Aktivieren (A=0)] [Sperren (A=2)] │
│         ▢ AKTIV      [Pause (A=1)]     [Sperren (A=2)] │
│                                                         │
│ Status-Variablen (-127…+127):                           │
│  A=2 (ActiveListen)                                     │
│  B=0   C=0   D=0   E=0   F=0   G=0   H=0   I=0         │
│  J=0   K=0   L=0   M=0   N=0   O=0   P=0   Q=0         │
│  R=0   S=0   T=0   U=0   V=0   W=0                     │
│  X=1 (Repeat)  Y=0 (KeyDuration)  Z=0 (Pause)          │
│                                                         │
│ Berechnete Lesewerte (letztes Event):                   │
│  DeltaData2=0  DD2Positive=0  DD2Negative=0             │
└─────────────────────────────────────────────────────────┘
```

**Variable-A-Steuerung:**
- Farbiges Status-Icon: Rot (■ A=2), Gelb (▤ A=1), Grün (▢ A=0).
- Schaltflächen rufen `PUT /api/v1/status/variables/A` auf.
- **Wenn A=2:** nur Button "Aktivieren" ist aktiv.
- Reservierte Variablen werden mit Alias in Klammern angezeigt.
- Berechnete `DD*`-Werte sind read-only und werden nach jedem MIDI-Event aktualisiert.
- Alle Variablen werden über SignalR (`/hubs/status`, `VariableChanged`) live aktualisiert.
- Latenz-Wert kommt von `GET /api/v1/status`.

---

## 3. API-Kommunikation

### 3.1 HTTP-Client

- `HttpClient` mit `BaseAddress = http://localhost:5173`.
- Retry-Policy (Polly): 3 Versuche, exponentielles Backoff.
- Timeout: 5 Sekunden pro Request.
- Verbindungsstatus wird periodisch (5 s) via `GET /api/v1/status` geprüft.

### 3.2 SignalR

- `HubConnectionBuilder` mit automatischem Reconnect.
- Bei Verbindungsverlust: Status-Indikator auf Rot; Reconnect im Hintergrund.

---

## 4. Konfiguration des Frontends

`appsettings.json` im App-Verzeichnis:

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

## 5. Technologie-Stack

| Bereich | Technologie |
|---|---|
| UI-Framework | WPF (.NET 10) |
| MVVM | CommunityToolkit.Mvvm |
| HTTP-Client | `System.Net.Http.HttpClient` + Polly |
| SignalR-Client | `Microsoft.AspNetCore.SignalR.Client` |
| JSON | `System.Text.Json` |
| DI | `Microsoft.Extensions.DependencyInjection` |
