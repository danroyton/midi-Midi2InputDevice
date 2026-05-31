# MidiController – Overall Architecture

## Overview

**MidiController** receives MIDI events from physical MIDI devices and converts them into system inputs (keystrokes). Backend and frontend run **in a single shared process** (`MidiController.Frontend.exe`). The backend (Kestrel host) is started in-process when the WPF application launches and communicates with the UI via the local REST API and SignalR.

```
┌─────────────────────────────────────────────────────────────────┐
│                     User (Windows)                              │
└────────────────────────────┬────────────────────────────────────┘
                             │ Mouse / Keyboard / Tray
┌────────────────────────────▼────────────────────────────────────┐
│            MidiController.Frontend.exe                          │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  WPF UI (.NET 10-windows)                               │    │
│  │  Status · MIDI Log · Devices · Mappings · Templates     │    │
│  │  KeyboardTest · Tray Icon                               │    │
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

## Project Layers

| Project | Technology | Responsibility |
|---|---|---|
| `MidiController.Domain` | .NET 10 | Domain models (`MidiEvent`, `Trigger`, `Profile`, variables, enums, interfaces) |
| `MidiController.Engine` | .NET 10 | Mapping engine, trigger executor, gate logic, delta tracking, variable store |
| `MidiController.Infrastructure` | .NET 10 / Win32 | MIDI input/output (NAudio), `SendInput` (P/Invoke), JSON config store |
| `MidiController.Host` | ASP.NET Core .NET 10 | Kestrel, REST API, SignalR hubs, DI configuration, `BackendStartup` entry point |
| `MidiController.Frontend` | WPF .NET 10-windows | Main application; starts backend in-process, provides the UI |

## Startup Sequence

```
App.xaml.cs → OnStartup()
  1. BackendHostService.StartAsync()        ← starts Kestrel in-process
  2. Build ServiceProvider                  ← register ViewModels, Services
  3. TrayService.Initialize()               ← create NotifyIcon
  4. Create and show MainWindow
  5. StatusViewModel connects to SignalR
```

## In-Process Backend Hosting

The `BackendStartup` entry point in `MidiController.Host` encapsulates the complete backend infrastructure:

```csharp
// MidiController.Host/BackendStartup.cs
public static Task StartAsync(
    string configPath,
    string url = "http://localhost:5000",
    CancellationToken ct = default)
```

Advantages:
- **One process, one EXE** – no separate backend launch required
- **No IPC complexity** – direct in-memory channel possible
- **Simple distribution** – single-file publish is sufficient

## Data Flow

```
Physical MIDI Device
  │  USB/DIN
  ▼
NAudio MidiIn.MessageReceived
  │
  ▼  Channel<MidiEvent> (cap=512, lock-free)
MappingEngine
  │  Evaluate triggers → check gate → read/write variables
  │  Pre-assignments + Pre-MIDI send
  │  Evaluate condition blocks
  │  Execute action blocks (SendInput)
  │  Post-assignments + Post-MIDI send
  ▼
Win32 SendInput
  │
  ▼
Windows keyboard input in the active application

In parallel:
MidiEvent → SignalR Hub /hubs/midilog → WPF MIDI Log View
Variable changes → SignalR Hub /hubs/status → WPF Status View
```

## Configuration Files

| File | Location | Purpose |
|---|---|---|
| `appsettings.json` | next to EXE | Frontend configuration (backend URL) |
| `appsettings.backend.json` | next to EXE | Backend configuration (DataPath, Kestrel URL) |
| `profiles\{name}.json` | `%APPDATA%\MidiController\` | Trigger/mapping profiles |
| `templates\{name}.json` | `%APPDATA%\MidiController\` | Reusable condition/action templates |

## Architecture Decisions

Formal justifications in the ADR files:

| ADR | Decision |
|---|---|
| [ADR-001](adr/ADR-001-midi-library.md) | NAudio as MIDI input library |
| [ADR-002](adr/ADR-002-midi-mapper-service.md) | Mapper as a standalone background service |
| [ADR-003](adr/ADR-003-virtual-midi-devices.md) | Virtual MIDI ports (still pending) |
| [ADR-004](adr/ADR-004-input-injection.md) | Win32 `SendInput` instead of virtual input devices |
| [ADR-005](adr/ADR-005-frontend-technology.md) | WPF as the frontend technology |
| [ADR-006](adr/ADR-006-ipc-api.md) | REST + SignalR as the IPC channel |
