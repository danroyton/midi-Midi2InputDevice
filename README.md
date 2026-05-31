# MidiController – Midi2InputDevice

Verbindet physikalische MIDI-Geräte (Keyboards, Drumcomputer, Encoder-Controller, …) mit Windows und wandelt MIDI-Events in beliebige Tastatur-Eingaben um – ohne Treiber oder Drittanbieter-Middleware.

> **Aktuelle Version: v0.3**  
> Single-EXE für Windows x64 – kein Installer, keine .NET-Laufzeit erforderlich.

---

## Inhaltsverzeichnis

- [Features](#features)
- [Schnellstart](#schnellstart)
- [Architektur](#architektur)
- [Konfiguration](#konfiguration)
- [Entwicklung](#entwicklung)
- [Release erstellen](#release-erstellen)
- [Roadmap](#roadmap)
- [Einschränkungen](#einschränkungen)

---

## Features

### MIDI-Verarbeitung
- Empfang von allen angeschlossenen physikalischen MIDI-Geräten (NAudio `MidiIn`)
- Automatischer Reconnect bei Gerätewechsel
- Unterstützte Event-Typen: `NoteOn`, `NoteOff`, `ControlChange`, `ProgramChange`, `PitchBend`
- Lock-freie Verarbeitung via `System.Threading.Channels`

### Mapping-Engine
- Trigger konfigurierbar nach: Gerät, Event-Typ, Kanal, Data1-Filter
- **Zustandsvariablen A–Z** (−127…+127) für komplexe Steuerlogik
- **Delta-Tracking**: `V`/`W` halten die Differenz zum vorherigen Event
- **Prüfblöcke**: UND zwischen Blöcken, ODER innerhalb eines Blocks
- **Globale Pre/Post-Phase**: Variablen setzen und MIDI-Befehle senden vor/nach Aktionen
- **ELSE-Zweige** bei fehlgeschlagenen Prüfblöcken
- **Aktivierungs-Gate** (Variable `A`): `0`=aktiv, `1`=pausiert, `2`=gesperrt

### MIDI-Ausgabe
- MIDI-Befehle an physikalische MIDI-Output-Geräte senden (als Pre/Post-Schritte in Triggern)
- Unterstützte Typen: `NoteOn`, `NoteOff`, `ControlChange`, `ProgramChange`, `PitchBend`

### Eingabe-Injektion
- Win32 `SendInput` (P/Invoke): Einzel-Tasten und Kombinationen
- `X` = Wiederholungen, `Y` = Hold-Dauer (ms), `Z` = Pause nach Tastendruck (ms)
- Strukturierter Tastenkombo-Editor: bis zu 2 Modifier + 1 Haupttaste

### Konfiguration & Templates
- Profile gespeichert als JSON unter `%APPDATA%\MidiController\`
- Wiederverwendbare Condition- und Action-Templates
- Trigger mit eigenem Anzeigenamen
- Live-Aktivierung ohne Neustart

### Frontend (WPF)
- **Status-View**: Gate-Steuerung, Echtzeit-Variablen-Tabelle (A–Z)
- **MIDI-Log-View**: Live-Stream aller MIDI-Events, Gerätefilter
- **Devices-View**: Physikalische MIDI-Geräte verbinden/trennen
- **Mappings-View**: Vollständiger Trigger-Editor (Pre/Post, Bedingungen, Aktionen, ELSE)
- **Templates-View**: Templates verwalten
- **Keyboard-Test-View**: Tastatureingaben live testen
- **System-Tray-Icon**: Blinkt bei MIDI-Aktivität, farbkodiert nach Gate-Status
- Einzel-EXE: Backend (Kestrel) läuft in-process im Frontend-Prozess

---

## Schnellstart

### Option A – Fertige EXE (empfohlen)

1. [Neuesten Release](https://github.com/danroyton/midi-Midi2InputDevice/releases/latest) herunterladen
2. ZIP entpacken
3. `MidiController.Frontend.exe` starten
4. Im **Status**-Tab auf **Aktivieren** klicken
5. Im **Devices**-Tab MIDI-Gerät öffnen
6. Im **Mappings**-Tab Profil anlegen und Trigger konfigurieren

Kein Installer, keine .NET-Runtime notwendig.

### Option B – Aus Quellcode

```bash
git clone https://github.com/danroyton/midi-Midi2InputDevice.git
cd Midi2InputDevice
dotnet run --project MidiController.Frontend
```

---

## Architektur

Backend (Kestrel) und Frontend (WPF) laufen **im gleichen Prozess**:

```
MidiController.Frontend.exe
├─ WPF-UI  ──────────────────────────────────── Benutzeroberfläche
└─ BackendHostService (Kestrel :5000)
   ├─ REST-API  /api/...
   ├─ SignalR   /hubs/status
   └─ SignalR   /hubs/midilog
        │
        ├─ MidiInputService  (NAudio, physikalische Geräte)
        ├─ MidiOutputService (NAudio, MIDI-Ausgabe)
        ├─ MappingEngine     (Trigger-Auswertung)
        └─ JsonConfigStore   (%APPDATA%\MidiController\)
```

Projektstruktur:

| Projekt | Beschreibung |
|---|---|
| `MidiController.Domain` | Domänenmodelle, Interfaces, Enums |
| `MidiController.Engine` | Mapping-Engine, Trigger-Executor, Gate, Delta-Tracking |
| `MidiController.Infrastructure` | MIDI-Input/Output (NAudio), SendInput (Win32), JSON-Config |
| `MidiController.Host` | ASP.NET Core, REST-API, SignalR-Hubs, `BackendStartup` |
| `MidiController.Frontend` | WPF-App, integriert den Backend-Host in-process |

---

## Konfiguration

### appsettings.json (Frontend)
```json
{
  "Backend": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

### appsettings.backend.json (Backend, neben der EXE)
```json
{
  "MidiController": {
    "DataPath": "%APPDATA%\\MidiController",
    "EventChannelCapacity": 512
  },
  "Urls": "http://localhost:5000"
}
```

Profile werden unter `%APPDATA%\MidiController\profiles\` als JSON gespeichert.

---

## Entwicklung

### Voraussetzungen
- **Windows 10/11** (64-Bit)
- **Visual Studio 2022/2026** mit Workloads:
  - `.NET Desktop Development`
  - `ASP.NET and Web Development`
- **.NET 10 SDK**

### Bauen & Starten

```bash
# Debug-Start (Frontend startet Backend in-process)
dotnet run --project MidiController.Frontend

# Tests
dotnet test

# Release-Build
dotnet build -c Release

# Single-File Publish
dotnet publish MidiController.Frontend\MidiController.Frontend.csproj \
  /p:PublishProfile=SingleFile -c Release
```

Ausgabe: `MidiController.Frontend\bin\Release\publish\MidiController.Frontend.exe`

---

## Release erstellen

Siehe [`docs/RELEASE.md`](MidiController/docs/RELEASE.md) für die vollständige Anleitung zum Erstellen eines GitHub-Releases.

Kurzübersicht:
1. Version in `CHANGELOG` vermerken
2. Git-Tag setzen: `git tag v0.3.0`
3. GitHub Actions Workflow `release.yml` baut und veröffentlicht automatisch

---

## Roadmap

| Version | Ziel |
|---|---|
| ✅ v0.1 | Grundgerüst: Backend, Engine, REST-API, WPF-Frontend |
| ✅ v0.2 | Tray-Icon, Tastatur-Test-View, strukturierter Tastenkombo-Editor |
| ✅ v0.3 | Vollständiger Trigger-Editor (Pre/Post, Bedingungen, ELSE), MIDI-Ausgabe, Single-EXE |
| 🔜 v0.4 | Virtuelle MIDI-Ports (loopMIDI / Windows MIDI Services) |
| 🔜 v0.5 | Linux-Backend + Avalonia-Frontend (cross-platform) |
| 🔜 v1.0 | Windows-Dienst-Betrieb, Installer (MSI), vollständige Dokumentation |

---

## Einschränkungen

| Einschränkung | Details |
|---|---|
| **Windows only** | `SendInput` und NAudio `MidiIn` sind Windows-spezifisch |
| **Keine virtuellen MIDI-Ports** | loopMIDI-Integration noch ausstehend (v0.4) |
| **Port 5000 fest** | Backend läuft immer auf `http://localhost:5000`; konfigurierbar in `appsettings.backend.json` |
| **Keine Authentifizierung** | REST-API ohne Auth; nur für lokalen Betrieb |
| **SendInput im Dienst-Modus** | Als Windows-Dienst (Session 0) ohne Session-Weiterleitung nicht nutzbar |
