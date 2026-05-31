# MidiController – Gesamtarchitektur

## Überblick

**MidiController** empfängt MIDI-Events von physikalischen MIDI-Geräten und wandelt sie in System-Eingaben (Tastendrücke) um. Backend und Frontend laufen **in einem gemeinsamen Prozess** (`MidiController.Frontend.exe`). Das Backend (Kestrel-Host) wird beim Start der WPF-App in-process gestartet und kommuniziert mit der Oberfläche über die lokale REST-API und SignalR.

```
┌─────────────────────────────────────────────────────────────────┐
│                     Benutzer (Windows)                          │
└────────────────────────────┬────────────────────────────────────┘
                             │ Maus / Tastatur / Tray
┌────────────────────────────▼────────────────────────────────────┐
│            MidiController.Frontend.exe                          │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  WPF-UI (.NET 10-windows)                               │    │
│  │  Status · MIDI-Log · Devices · Mappings · Templates     │    │
│  │  KeyboardTest · Tray-Icon                               │    │
│  └─────────────────┬───────────────────────────────────────┘    │
│                    │ REST  http://localhost:5000                 │
│                    │ SignalR /hubs/status /hubs/midilog          │
│  ┌─────────────────▼───────────────────────────────────────┐    │
│  │  BackendHostService  (Kestrel in-process)               │    │
│  │                                                         │    │
│  │  ┌─────────────┐  ┌──────────────────┐  ┌───────────┐  │    │
│  │  │ MidiInput   │  │  MappingEngine   │  │ SendInput │  │    │
│  │  │ Service     │─▶│  (Trigger/Gate/  │─▶│ (Win32)   │  │    │
│  │  │ (NAudio)    │  │   Variables A-Z) │  └───────────┘  │    │
│  │  └─────────────┘  └──────────────────┘                 │    │
│  │  ┌─────────────┐  ┌──────────────────┐                 │    │
│  │  │ MidiOutput  │  │  JsonConfigStore │                 │    │
│  │  │ Service     │  │  (%APPDATA%\     │                 │    │
│  │  │ (NAudio)    │  │   MidiController)│                 │    │
│  │  └─────────────┘  └──────────────────┘                 │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

## Projekt-Schichten

| Projekt | Technologie | Verantwortung |
|---|---|---|
| `MidiController.Domain` | .NET 10 | Domänenmodelle (`MidiEvent`, `Trigger`, `Profile`, Variablen, Enums, Interfaces) |
| `MidiController.Engine` | .NET 10 | Mapping-Engine, Trigger-Executor, Gate-Logik, Delta-Tracking, Variable Store |
| `MidiController.Infrastructure` | .NET 10 / Win32 | MIDI-Input/Output (NAudio), `SendInput` (P/Invoke), JSON-Konfigurations-Store |
| `MidiController.Host` | ASP.NET Core .NET 10 | Kestrel, REST-API, SignalR-Hubs, DI-Konfiguration, `BackendStartup`-Einstiegspunkt |
| `MidiController.Frontend` | WPF .NET 10-windows | Hauptanwendung; startet Backend in-process, stellt die UI bereit |

## Startup-Sequenz

```
App.xaml.cs → OnStartup()
  1. BackendHostService.StartAsync()        ← startet Kestrel in-process
  2. ServiceProvider aufbauen               ← ViewModels, Services registrieren
  3. TrayService.Initialize()               ← NotifyIcon anlegen
  4. MainWindow erstellen und anzeigen
  5. StatusViewModel verbindet SignalR
```

## In-Process-Backend-Hosting

Der `BackendStartup`-Einstiegspunkt in `MidiController.Host` kapselt die komplette Backend-Infrastruktur:

```csharp
// MidiController.Host/BackendStartup.cs
public static Task StartAsync(
    string configPath,
    string url = "http://localhost:5000",
    CancellationToken ct = default)
```

Vorteile:
- **Ein Prozess, eine EXE** – kein separater Backend-Start nötig
- **Keine IPC-Komplexität** – direkter In-Memory-Kanal möglich
- **Einfache Verteilung** – Single-File-Publish genügt

## Datenfluss

```
Physikalisches MIDI-Gerät
  │  USB/DIN
  ▼
NAudio MidiIn.MessageReceived
  │
  ▼  Channel<MidiEvent> (cap=512, lock-free)
MappingEngine
  │  Trigger auswerten → Gate prüfen → Variablen lesen/schreiben
  │  Pre-Zuweisungen + Pre-MIDI-Send
  │  Bedingungsblöcke evaluieren
  │  Aktionsblöcke ausführen (SendInput)
  │  Post-Zuweisungen + Post-MIDI-Send
  ▼
Win32 SendInput
  │
  ▼
Windows-Tastatureingabe in aktiver Anwendung

Parallel:
MidiEvent → SignalR Hub /hubs/midilog → WPF MIDI-Log-View
Variablen-Änderungen → SignalR Hub /hubs/status → WPF Status-View
```

## Konfigurationsdateien

| Datei | Ort | Zweck |
|---|---|---|
| `appsettings.json` | neben EXE | Frontend-Konfiguration (Backend-URL) |
| `appsettings.backend.json` | neben EXE | Backend-Konfiguration (DataPath, Kestrel-URL) |
| `profiles\{name}.json` | `%APPDATA%\MidiController\` | Trigger/Mapping-Profile |
| `templates\{name}.json` | `%APPDATA%\MidiController\` | Wiederverwendbare Bedingung-/Aktions-Templates |

## Architekturentscheidungen

Formale Begründungen in den ADR-Dateien:

| ADR | Entscheidung |
|---|---|
| [ADR-001](adr/ADR-001-midi-library.md) | NAudio als MIDI-Eingangs-Bibliothek |
| [ADR-002](adr/ADR-002-midi-mapper-service.md) | Mapper als eigenständiger Hintergrund-Service |
| [ADR-003](adr/ADR-003-virtual-midi-devices.md) | Virtuelle MIDI-Ports (noch ausstehend) |
| [ADR-004](adr/ADR-004-input-injection.md) | Win32 `SendInput` statt virtueller Eingabegeräte |
| [ADR-005](adr/ADR-005-frontend-technology.md) | WPF als Frontend-Technologie |
| [ADR-006](adr/ADR-006-ipc-api.md) | REST + SignalR als IPC-Kanal |
