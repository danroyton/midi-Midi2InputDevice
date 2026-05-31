# MidiController – Midi2InputDevice

Connects physical MIDI devices (keyboards, drum machines, encoder controllers, …) to Windows and converts MIDI events into arbitrary keyboard input – without drivers or third-party middleware.

> **Current version: v0.3**
> Single-EXE for Windows x64 – no installer, no .NET runtime required.

---

## Table of Contents

- [Features](#features)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Configuration](#configuration)
- [Development](#development)
- [Creating a Release](#creating-a-release)
- [Roadmap](#roadmap)
- [Limitations](#limitations)

---

## Features

### MIDI Processing
- Receive input from all connected physical MIDI devices (NAudio `MidiIn`)
- Automatic reconnect on device change
- Supported event types: `NoteOn`, `NoteOff`, `ControlChange`, `ProgramChange`, `PitchBend`
- Lock-free processing via `System.Threading.Channels`

### Mapping Engine
- Triggers configurable by: device, event type, channel, Data1 filter
- **State variables A–Z** (−127…+127) for complex control logic
- **Delta tracking**: `V`/`W` hold the difference from the previous event
- **Condition blocks**: AND between blocks, OR within a block
- **Global pre/post phase**: set variables and send MIDI commands before/after actions
- **ELSE branches** for failed condition blocks
- **Activation gate** (variable `A`): `0`=active, `1`=paused, `2`=locked

### MIDI Output
- Send MIDI commands to physical MIDI output devices (as pre/post steps in triggers)
- Supported types: `NoteOn`, `NoteOff`, `ControlChange`, `ProgramChange`, `PitchBend`

### Input Injection
- Win32 `SendInput` (P/Invoke): single keys and combinations
- `X` = repetitions, `Y` = hold duration (ms), `Z` = pause after key press (ms)
- Structured key combo editor: up to 2 modifiers + 1 main key

### Configuration & Templates
- Profiles stored as JSON under `%APPDATA%\MidiController\`
- Reusable condition and action templates
- Triggers with custom display names
- Live activation without restart

### Frontend (WPF)
- **Status view**: gate control, real-time variable table (A–Z)
- **MIDI log view**: live stream of all MIDI events, device filter
- **Devices view**: connect/disconnect physical MIDI devices
- **Mappings view**: full trigger editor (pre/post, conditions, actions, ELSE)
- **Templates view**: manage templates
- **Keyboard test view**: test keyboard input live
- **System tray icon**: blinks on MIDI activity, color-coded by gate status
- Single-EXE: backend (Kestrel) runs in-process within the frontend process

---

## Quick Start

### Option A – Pre-built EXE (recommended)

1. Download the [latest release](https://github.com/danroyton/midi-Midi2InputDevice/releases/latest)
2. Extract the ZIP
3. Launch `MidiController.Frontend.exe`
4. In the **Status** tab click **Activate**
5. In the **Devices** tab open your MIDI device
6. In the **Mappings** tab create a profile and configure triggers

No installer, no .NET runtime required.

### Option B – From source

```bash
git clone https://github.com/danroyton/midi-Midi2InputDevice.git
cd Midi2InputDevice
dotnet run --project MidiController.Frontend
```

---

## Architecture

Backend (Kestrel) and frontend (WPF) run **in the same process**:

```
MidiController.Frontend.exe
├─ WPF UI  ──────────────────────────────────── User interface
└─ BackendHostService (Kestrel :5000)
   ├─ REST API  /api/...
   ├─ SignalR   /hubs/status
   └─ SignalR   /hubs/midilog
        │
        ├─ MidiInputService  (NAudio, physical devices)
        ├─ MidiOutputService (NAudio, MIDI output)
        ├─ MappingEngine     (trigger evaluation)
        └─ JsonConfigStore   (%APPDATA%\MidiController\)
```

Project structure:

| Project | Description |
|---|---|
| `MidiController.Domain` | Domain models, interfaces, enums |
| `MidiController.Engine` | Mapping engine, trigger executor, gate, delta tracking |
| `MidiController.Infrastructure` | MIDI input/output (NAudio), SendInput (Win32), JSON config |
| `MidiController.Host` | ASP.NET Core, REST API, SignalR hubs, `BackendStartup` |
| `MidiController.Frontend` | WPF app, integrates the backend host in-process |

---

## Configuration

### appsettings.json (Frontend)
```json
{
  "Backend": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

### appsettings.backend.json (Backend, next to the EXE)
```json
{
  "MidiController": {
    "DataPath": "%APPDATA%\\MidiController",
    "EventChannelCapacity": 512
  },
  "Urls": "http://localhost:5000"
}
```

Profiles are stored as JSON under `%APPDATA%\MidiController\profiles\`.

---

## Development

### Prerequisites
- **Windows 10/11** (64-bit)
- **Visual Studio 2022/2026** with workloads:
  - `.NET Desktop Development`
  - `ASP.NET and Web Development`
- **.NET 10 SDK**

### Build & Run

```bash
# Debug run (frontend starts backend in-process)
dotnet run --project MidiController.Frontend

# Tests
dotnet test

# Release build
dotnet build -c Release

# Single-file publish
dotnet publish MidiController.Frontend\MidiController.Frontend.csproj \
  /p:PublishProfile=SingleFile -c Release
```

Output: `MidiController.Frontend\bin\Release\publish\MidiController.Frontend.exe`

---

## Creating a Release

See [`docs/RELEASE.md`](MidiController/docs/RELEASE.md) for the full guide on creating a GitHub release.

Quick overview:
1. Record the version in the `CHANGELOG`
2. Set a Git tag: `git tag v0.3.0`
3. The GitHub Actions workflow `release.yml` builds and publishes automatically

---

## Roadmap

| Version | Goal |
|---|---|
| ✅ v0.1 | Foundation: backend, engine, REST API, WPF frontend |
| ✅ v0.2 | Tray icon, keyboard test view, structured key combo editor |
| ✅ v0.3 | Full trigger editor (pre/post, conditions, ELSE), MIDI output, single-EXE |
| 🔜 v0.4 | Virtual MIDI ports (loopMIDI / Windows MIDI Services) |
| 🔜 v0.5 | Linux backend + Avalonia frontend (cross-platform) |
| 🔜 v1.0 | Windows service mode, installer (MSI), full documentation |

---

## Limitations

| Limitation | Details |
|---|---|
| **Windows only** | `SendInput` and NAudio `MidiIn` are Windows-specific |
| **No virtual MIDI ports** | loopMIDI integration pending (v0.4) |
| **Port 5000 fixed** | Backend always runs on `http://localhost:5000`; configurable in `appsettings.backend.json` |
| **No authentication** | REST API without auth; local use only |
| **SendInput in service mode** | Not usable as a Windows service (Session 0) without session forwarding |
