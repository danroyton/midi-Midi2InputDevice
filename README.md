# Midi2InputDevice

## Ziel des Projekts

MidiController verbindet physikalische MIDI-Geräte (Keyboards, Drumcomputer, Encoder-Controller, …) mit dem Windows-Betriebssystem und ermöglicht es, MIDI-Events in beliebige Tastatur- oder Maus-Eingaben umzusetzen – ohne manuelle Treiber oder Drittanbieter-Middleware. Damit lassen sich z. B. Lautstärke, Mediensteuerung, Makros oder Spielsteuerung über MIDI-Hardware steuern.

Das Projekt besteht aus einem **Backend-Dienst** (ASP.NET Core, .NET 10) und einer **WPF-Konfigurations-App** (.NET 10, Windows).

---

## Komponenten

| Komponente | Beschreibung |
|---|---|
| **MidiController.Domain** | Domänenmodelle (`MidiEvent`, `Trigger`, `Profile`, Variablen A–Z, Enums) |
| **MidiController.Engine** | Mapping-Engine, Trigger-Auswertung, Gate-Logik, Delta-Tracking, Variable Store |
| **MidiController.Infrastructure** | MIDI-Eingangsservice (NAudio), Windows-Eingabe-Injektion (`SendInput`), Konfigurations-Store (JSON) |
| **MidiController.Host** | ASP.NET Core Host, REST-API, SignalR-Hubs, Startup-Verdrahtung |
| **MidiController.Frontend** | WPF-App (.NET 10-windows): Status, MIDI-Log, Mappings, Templates, Devices |

---

## Features

### MIDI-Verarbeitung
- Empfang von MIDI-Events von allen angeschlossenen physikalischen Geräten (NAudio `MidiIn`)
- Unterstützte Event-Typen: `NoteOn`, `NoteOff`, `ControlChange`, `ProgramChange` u. a.
- Hochpriorität-Verarbeitung via `System.Threading.Channels` (kein Locking im Hot-Path)

### Mapping-Engine
- Flexible Trigger-Konfiguration: Gerät + Event-Typ + Kanal + Data1-Filter
- **Zustandsvariablen A–Z** (Wertebereich −127…+127) für komplexe Steuerlogik
- **Delta-Tracking**: `V` (DeltaData1) und `W` (DeltaData2) halten die Differenz zum vorherigen Event
- **Berechnete Lesewerte**: `DD1PosAbs`, `DD1NegAbs`, `DD2PosAbs`, `DD2NegAbs`, `DD1Pos`, `DD1Neg`, `DD2Pos`, `DD2Neg`
- **Prüfblöcke** (UND-verknüpft zwischen Blöcken, ODER innerhalb eines Blocks)
- **Mehrere sequenzielle Aktionen** pro Trigger, je mit eigenem Tastendruck, X/Y/Z-Quellen und State-Zuweisungen
- **Globale Pre-/Post-Zuweisungen** für Sperr- und Signalisierungsmuster
- **ELSE-Zweige** bei fehlgeschlagenen Prüfblöcken
- **Aktivierungs-Gate** (Variable `A`): `0` = aktiv, `1` = pausiert, `2` = gesperrt (Default)

### Eingabe-Injektion
- Win32 `SendInput` (P/Invoke): Einzel-Tasten, Kombinationen, Key-Down+Hold+Up
- `X` = Wiederholungen, `Y` = Hold-Dauer (ms), `Z` = Pause nach Tastendruck (ms)

### Konfiguration & Templates
- Profile gespeichert als JSON unter `%ProgramData%\MidiController\profiles\`
- Wiederverwendbare **ConditionBlock**- und **ActionBlock-Templates**
- Live-Aktivierung eines Profils ohne Neustart

### API & Echtzeit-Streaming
- REST-API: Profile, Trigger, Devices, Status, Variablen
- **SignalR-Hubs**: `/hubs/status` (Variablen-Livestream) und `/hubs/midilog` (MIDI-Rohdaten)

### Frontend (WPF)
- **Status-View**: Gate-Steuerung (Aktivieren / Pause / Sperren), Echtzeit-Variablen-Tabelle (A–Z)
- **MIDI-Log-View**: Live-Stream aller MIDI-Events, Gerätefilter-ComboBox (nur verbundene Geräte), bis zu 1.000 Zeilen
- **Devices-View**: Physikalische MIDI-Geräte auflisten, verbinden und trennen
- **Mappings-View**: Trigger-Übersicht mit Gerätefilter-ComboBox (Editor in Entwicklung)
- **Templates-View**: Templates auflisten und löschen
- Profil-Dropdown + Aktivieren-Button in der Top-Bar
- Farbiger Verbindungsindikator (Grün/Gelb/Orange/Rot)

---

## Einschränkungen (aktuell)

| Einschränkung | Details |
|---|---|
| **Windows only** | `SendInput` und NAudio `MidiIn` sind Windows-spezifisch; kein Linux/macOS-Support |
| **Keine virtuellen MIDI-Ports** | `VirtualMidiPortService` ist noch nicht implementiert (loopMIDI-Integration ausstehend) |
| **Kein Trigger-Editor im Frontend** | Die Mappings-View zeigt nur eine Statusmeldung; der vollständige Trigger-Editor ist noch nicht gebaut |
| **Kein Tray-Icon** | Die App läuft als normales Fenster; kein Systray-Icon vorhanden |
| **SendInput und Dienst-Session** | Als Windows-Dienst (Session 0) funktioniert `SendInput` nicht ohne zusätzliche Session-Weiterleitung |
| **Keine Authentifizierung** | Die REST-API ist ohne Auth erreichbar; nur für lokalen Betrieb gedacht |
| **Kein Hot-Reload der Konfiguration** | Profiländerungen im Dateisystem erfordern eine neue Aktivierung über die API |

---

## Voraussetzungen

### Laufzeit
- **Windows 10 / 11** (64-Bit)
- **.NET 10 Runtime** ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- Mindestens ein physikalisches MIDI-Gerät (USB oder DIN, über Treiber eingebunden)

### Entwicklung
- **Visual Studio 2022 / 2026** (Community oder höher) mit Workloads:
  - `.NET Desktop Development` (für WPF-Frontend)
  - `ASP.NET and Web Development` (für Host)
- **.NET 10 SDK**
- **Git**

---

## Installation & Start

### 1. Repository klonen

```bash
git clone https://github.com/danroyton/midi-Midi2InputDevice.git
cd midi-Midi2InputDevice
```

### 2. Solution bauen

```bash
dotnet build Midi2InputDevice.slnx
```

### 3. Backend starten

```bash
cd MidiController.Host
dotnet run
```

Der Host lauscht standardmäßig auf `http://localhost:5000`.  
Die REST-API ist unter `http://localhost:5000/api/` erreichbar.

### 4. Frontend starten

```bash
cd MidiController.Frontend
dotnet run
```

Oder in Visual Studio das Projekt `MidiController.Frontend` als Startprojekt setzen und starten.

### 5. Als Windows-Dienst installieren (optional)

```powershell
sc.exe create MidiController binPath="C:\Pfad\zu\MidiController.Host.exe"
sc.exe start MidiController
```

> **Hinweis:** Im Dienst-Modus ist `SendInput` nur in der interaktiven Benutzersession wirksam (Session 1). Für produktiven Einsatz als Dienst wird eine Session-Weiterleitung benötigt (siehe ADR-002).

### 6. Profil anlegen

Profile werden als JSON-Dateien unter `%ProgramData%\MidiController\profiles\` abgelegt.  
Ein Beispiel-Profil befindet sich in `MidiController\docs\SPEC_BACKEND.md` (Abschnitt 2.6).

Nach dem Start ist das Aktivierungs-Gate standardmäßig **gesperrt** (`A=2`).  
Über das Frontend (Status-View → „Aktivieren") oder die API (`PUT /api/status/variables/A` mit `{ "value": 0 }`) wird die Verarbeitung freigegeben.

---

## Roadmap

### v0.2 – Tray-Integration
- **System-Tray-Icon** für den Host-Prozess (Windows NotifyIcon)
- Icon blinkt / wechselt Farbe, wenn MIDI-Signale aktiv verarbeitet werden
- Kontextmenü: Aktivieren / Pause / Sperren / Beenden

### v0.3 – Trigger-Editor
- Vollständiger Trigger-Editor im WPF-Frontend (Prüfblöcke, Aktionen, ELSE-Zweige, Templates)
- Drag & Drop für Prüfblock-Reihenfolge
- Rechtsklick im MIDI-Log → „Trigger für dieses Event anlegen" (vorausgefüllter Editor)

### v0.4 – Virtuelle MIDI-Ports
- Integration von **loopMIDI** (COM-Automation) für virtuelle Port-Erstellung
- Alternativ: Windows MIDI Services API (ab Windows 11 24H2)
- UI-Verwaltung in der Devices-View

### v0.5 – Linux-Backend + Avalonia-Frontend
- Backend-Portierung auf **Linux** mit **.NET 10** und **RtMidi.NET** statt NAudio
- Eingabe-Injektion unter Linux via `libevdev` / `uinput`
- Ersetzen der WPF-Oberfläche durch **Avalonia UI** (cross-platform)
- Gemeinsame ViewModel-Schicht (plattformunabhängig)

### v1.0 – Stabiler Windows-Dienst-Betrieb
- Vollständige Session-Weiterleitung für `SendInput` im Dienst-Modus (Session 0 → Session 1)
- Automatischer Reconnect bei MIDI-Gerätewechsel
- Installer (MSI / WiX)
- Vollständige Dokumentation und Beispiel-Profile


Das Projekt besteht aus einem **Backend-Dienst** (ASP.NET Core, .NET 10) und einer **WPF-Konfigurations-App** (.NET 10, Windows).

---

## Komponenten

| Komponente | Beschreibung |
|---|---|
| **MidiController.Domain** | Domänenmodelle (`MidiEvent`, `Trigger`, `Profile`, Variablen A–Z, Enums) |
| **MidiController.Engine** | Mapping-Engine, Trigger-Auswertung, Gate-Logik, Delta-Tracking, Variable Store |
| **MidiController.Infrastructure** | MIDI-Eingangsservice (NAudio), Windows-Eingabe-Injektion (`SendInput`), Konfigurations-Store (JSON) |
| **MidiController.Host** | ASP.NET Core Host, REST-API, SignalR-Hubs, Startup-Verdrahtung |
| **MidiControllerFrontend** | WPF-App (.NET 10-windows): Status, MIDI-Log, Mappings, Templates, Devices |

---

## Features

### MIDI-Verarbeitung
- Empfang von MIDI-Events von allen angeschlossenen physikalischen Geräten (NAudio `MidiIn`)
- Unterstützte Event-Typen: `NoteOn`, `NoteOff`, `ControlChange`, `ProgramChange` u. a.
- Hochpriorität-Verarbeitung via `System.Threading.Channels` (kein Locking im Hot-Path)

### Mapping-Engine
- Flexible Trigger-Konfiguration: Gerät + Event-Typ + Kanal + Data1-Filter
- **Zustandsvariablen A–Z** (Wertebereich −127…+127) für komplexe Steuerlogik
- **Delta-Tracking**: `V` (DeltaData1) und `W` (DeltaData2) halten die Differenz zum vorherigen Event
- **Berechnete Lesewerte**: `DD1PosAbs`, `DD1NegAbs`, `DD2PosAbs`, `DD2NegAbs`, `DD1Pos`, `DD1Neg`, `DD2Pos`, `DD2Neg`
- **Prüfblöcke** (UND-verknüpft zwischen Blöcken, ODER innerhalb eines Blocks)
- **Mehrere sequenzielle Aktionen** pro Trigger, je mit eigenem Tastendruck, X/Y/Z-Quellen und State-Zuweisungen
- **Globale Pre-/Post-Zuweisungen** für Sperr- und Signalisierungsmuster
- **ELSE-Zweige** bei fehlgeschlagenen Prüfblöcken
- **Aktivierungs-Gate** (Variable `A`): `0` = aktiv, `1` = pausiert, `2` = gesperrt (Default)

### Eingabe-Injektion
- Win32 `SendInput` (P/Invoke): Einzel-Tasten, Kombinationen, Key-Down+Hold+Up
- `X` = Wiederholungen, `Y` = Hold-Dauer (ms), `Z` = Pause nach Tastendruck (ms)

### Konfiguration & Templates
- Profile gespeichert als JSON unter `%ProgramData%\MidiController\profiles\`
- Wiederverwendbare **ConditionBlock**- und **ActionBlock-Templates**
- Live-Aktivierung eines Profils ohne Neustart

### API & Echtzeit-Streaming
- REST-API: Profile, Trigger, Devices, Status, Variablen
- **SignalR-Hubs**: `/hubs/status` (Variablen-Livestream) und `/hubs/midilog` (MIDI-Rohdaten)

### Frontend (WPF)
- **Status-View**: Gate-Steuerung (Aktivieren / Pause / Sperren), Echtzeit-Variablen-Tabelle (A–Z)
- **MIDI-Log-View**: Live-Stream aller MIDI-Events, Gerätefilter, bis zu 1.000 Zeilen
- **Devices-View**: Physikalische MIDI-Geräte auflisten
- **Mappings-View**: Trigger-Übersicht (Editor in Entwicklung)
- **Templates-View**: Templates auflisten und löschen
- Profil-Dropdown + Aktivieren-Button in der Top-Bar
- Farbiger Verbindungsindikator (Grün/Gelb/Orange/Rot)

---

## Einschränkungen (aktuell)

| Einschränkung | Details |
|---|---|
| **Windows only** | `SendInput` und NAudio `MidiIn` sind Windows-spezifisch; kein Linux/macOS-Support |
| **Keine virtuellen MIDI-Ports** | `VirtualMidiPortService` ist noch nicht implementiert (loopMIDI-Integration ausstehend) |
| **Kein Trigger-Editor im Frontend** | Die Mappings-View zeigt nur eine Statusmeldung; der vollständige Trigger-Editor ist noch nicht gebaut |
| **Kein Tray-Icon** | Die App läuft als normales Fenster; kein Systray-Icon vorhanden |
| **SendInput und Dienst-Session** | Als Windows-Dienst (Session 0) funktioniert `SendInput` nicht ohne zusätzliche Session-Weiterleitung |
| **Keine Authentifizierung** | Die REST-API ist ohne Auth erreichbar; nur für lokalen Betrieb gedacht |
| **Kein Hot-Reload der Konfiguration** | Profiländerungen im Dateisystem erfordern eine neue Aktivierung über die API |

---

## Voraussetzungen

### Laufzeit
- **Windows 10 / 11** (64-Bit)
- **.NET 10 Runtime** ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- Mindestens ein physikalisches MIDI-Gerät (USB oder DIN, über Treiber eingebunden)

### Entwicklung
- **Visual Studio 2022 / 2026** (Community oder höher) mit Workloads:
  - `.NET Desktop Development` (für WPF-Frontend)
  - `ASP.NET and Web Development` (für Host)
- **.NET 10 SDK**
- **Git**

---

## Installation & Start

### 1. Repository klonen

```bash
git clone https://github.com/<org>/MidiController.git
cd MidiController
```

### 2. Solution bauen

```bash
dotnet build MidiController.sln
```

### 3. Backend starten

```bash
cd MidiController.Host
dotnet run
```

Der Host lauscht standardmäßig auf `http://localhost:5000`.  
Die REST-API ist unter `http://localhost:5000/api/` erreichbar.

### 4. Frontend starten

```bash
cd MidiControllerFrontend
dotnet run
```

Oder in Visual Studio das Projekt `MidiControllerFrontend` als Startprojekt setzen und starten.

### 5. Als Windows-Dienst installieren (optional)

```powershell
sc.exe create MidiController binPath="C:\Pfad\zu\MidiController.Host.exe"
sc.exe start MidiController
```

> **Hinweis:** Im Dienst-Modus ist `SendInput` nur in der interaktiven Benutzersession wirksam (Session 1). Für produktiven Einsatz als Dienst wird eine Session-Weiterleitung benötigt (siehe ADR-002).

### 6. Profil anlegen

Profile werden als JSON-Dateien unter `%ProgramData%\MidiController\profiles\` abgelegt.  
Ein Beispiel-Profil befindet sich in `docs\SPEC_BACKEND.md` (Abschnitt 2.6).

Nach dem Start ist das Aktivierungs-Gate standardmäßig **gesperrt** (`A=2`).  
Über das Frontend (Status-View → „Aktivieren") oder die API (`PUT /api/status/variables/A` mit `{ "value": 0 }`) wird die Verarbeitung freigegeben.

---

## Roadmap

### v0.2 – Tray-Integration
- **System-Tray-Icon** für den Host-Prozess (Windows NotifyIcon)
- Icon blinkt / wechselt Farbe, wenn MIDI-Signale aktiv verarbeitet werden
- Kontextmenü: Aktivieren / Pause / Sperren / Beenden

### v0.3 – Trigger-Editor
- Vollständiger Trigger-Editor im WPF-Frontend (Prüfblöcke, Aktionen, ELSE-Zweige, Templates)
- Drag & Drop für Prüfblock-Reihenfolge
- Rechtsklick im MIDI-Log → „Trigger für dieses Event anlegen" (vorausgefüllter Editor)

### v0.4 – Virtuelle MIDI-Ports
- Integration von **loopMIDI** (COM-Automation) für virtuelle Port-Erstellung
- Alternativ: Windows MIDI Services API (ab Windows 11 24H2)
- UI-Verwaltung in der Devices-View

### v0.5 – Linux-Backend + Avalonia-Frontend
- Backend-Portierung auf **Linux** mit **.NET 10** und **RtMidi.NET** statt NAudio
- Eingabe-Injektion unter Linux via `libevdev` / `uinput`
- Ersetzen der WPF-Oberfläche durch **Avalonia UI** (cross-platform)
- Gemeinsame ViewModel-Schicht (plattformunabhängig)

### v1.0 – Stabiler Windows-Dienst-Betrieb
- Vollständige Session-Weiterleitung für `SendInput` im Dienst-Modus (Session 0 → Session 1)
- Automatischer Reconnect bei MIDI-Gerätewechsel
- Installer (MSI / WiX)
- Vollständige Dokumentation und Beispiel-Profile
