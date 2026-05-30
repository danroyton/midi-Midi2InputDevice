# MidiController – Gesamtarchitektur

## Übersicht

Die Applikation **MidiController** empfängt MIDI-Events von physikalischen MIDI-Geräten und wandelt sie in System-Eingaben (Tastendrücke, Maus-Aktionen) um. Über ein grafisches Frontend kann der Nutzer Geräte verwalten und Mappings konfigurieren.

```
┌─────────────────────────────────────────────────────────────────┐
│                          Benutzer                               │
└───────────────────────────┬─────────────────────────────────────┘
							│ UI-Interaktion
┌───────────────────────────▼─────────────────────────────────────┐
│              Frontend (WPF, .NET 10)                            │
│  - Device-Auswahl          - Mapping-Konfiguration              │
│  - Live-Log-View           - Profil-Verwaltung                  │
└───────────────────────────┬─────────────────────────────────────┘
							│ REST-API (HTTP/WebSocket)
┌───────────────────────────▼─────────────────────────────────────┐
│              Backend (ASP.NET Core Worker Service, .NET 10)     │
│                                                                 │
│  ┌──────────────┐   ┌──────────────────┐   ┌────────────────┐  │
│  │  MIDI Input  │──▶│  Event Pipeline  │──▶│ Input Injector │  │
│  │  Service     │   │  (Routing/Filter)│   │ (Win32/SendInput│  │
│  └──────────────┘   └──────────────────┘   └────────────────┘  │
│         │                    │                                  │
│  ┌──────▼──────┐   ┌─────────▼────────┐                        │
│  │  Virtual    │   │  Mapping Engine  │                        │
│  │  MIDI Ports │   │  (State Machine) │                        │
│  └─────────────┘   └──────────────────┘                        │
│                             │                                   │
│                    ┌────────▼─────────┐                         │
│                    │  Config Store    │                         │
│                    │  (appsettings /  │                         │
│                    │   profile JSON)  │                         │
│                    └──────────────────┘                         │
└─────────────────────────────────────────────────────────────────┘
```

## Komponenten

| Komponente | Technologie | Beschreibung |
|---|---|---|
| **MIDI Input Service** | NAudio / RtMidi.NET | Lauscht auf physikalische MIDI-Ports, gibt Events in die Pipeline |
| **Virtual MIDI Ports** | loopMIDI / Windows MIDI Services | Erstellt logische Ports für Multi-Consumer-Zugriff |
| **Event Pipeline** | System.Threading.Channels | Lock-freie, niedrig-latente Weitergabe von Events |
| **Mapping Engine** | Eigene Implementierung | Wertet Status-Variablen, Bedingungen und Aktions-Definitionen aus |
| **Input Injector** | Win32 SendInput API | Injiziert Tastendrücke/Maus-Events in das Betriebssystem |
| **Config Store** | JSON (System.Text.Json) | Speichert Profile, Device-Zuordnungen und Mappings |
| **REST-API** | ASP.NET Core Minimal API | Kommunikationsschnittstelle zum Frontend |
| **WebSocket Hub** | ASP.NET Core SignalR | Echtzeit-MIDI-Roh-Log an Frontend |
| **Frontend** | WPF (.NET 10) | Konfigurationsoberfläche |

## Latenz-Strategie

- **`System.Threading.Channels`** (bounded, Single-Consumer) für die Event-Pipeline – kein Kopieren, kein Locking.
- Der MIDI-Callback-Thread schreibt direkt in den Channel; ein dedizierter High-Priority-Thread liest und injiziert.
- Der Worker-Thread wird mit `ThreadPriority.Highest` und optional `ProcessPriorityClass.High` betrieben.
- Kein UI-Dispatch auf dem Hot-Path; Logging erfolgt asynchron über einen separaten Channel.

## Sicherheit / Isolation

- Backend läuft als Windows-Dienst (optional) mit eingeschränkten Rechten.
- Frontend kommuniziert nur über localhost REST/WebSocket.
- Eingaben werden nur injiziert, wenn ein aktives Profil geladen ist.

## Dateistruktur (Repo)

```
MidiController/
├── docs/
│   ├── ARCHITECTURE.md          ← diese Datei
│   ├── SPEC_BACKEND.md
│   ├── SPEC_FRONTEND.md
│   └── adr/
│       ├── ADR-001-midi-library.md
│       ├── ADR-002-midi-mapper-service.md
│       ├── ADR-003-virtual-midi-devices.md
│       ├── ADR-004-input-injection.md
│       ├── ADR-005-frontend-technology.md
│       └── ADR-006-ipc-api.md
├── KarentaMidi2DeviceInput/     ← Backend (Worker Service + API)
├── KarentaMidi2DeviceInput.UI/  ← Frontend (WPF)
└── KarentaMidi2DeviceInput.sln
```
