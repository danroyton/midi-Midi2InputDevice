# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [0.3.5] – 2025-06-01

### Added
- Full trigger editor: pre-phase and post-phase action blocks
- Condition blocks with AND (between blocks) and OR (within a block) logic
- ELSE branches for failed condition blocks
- MIDI output support in pre/post phases (`NoteOn`, `NoteOff`, `ControlChange`, `ProgramChange`, `PitchBend`)
- Global pre-assignments (step 0) executed before condition evaluation
- Global post-assignments executed after all actions
- `TriggerMatchMode`: `Variable`, `Data2`, `DeltaData2`, `DD2Positive`, `DD2Negative`
- `DD2Positive` / `DD2Negative` value sources for encoder direction handling without ELSE branches
- Single-file self-contained publish profile (`SingleFile`) — no .NET runtime required
- GitHub Actions workflow `release.yml` for automated build and release

### Changed
- Backend and frontend now run **in the same process** (Kestrel hosted in-process via `BackendHostService`)
- Trigger editor refactored to support multi-action sequences with per-action X/Y/Z sources

---

## [0.3.4] – 2025-05-31

### Added
- System tray icon with MIDI activity animation and gate-status colour coding
- Keyboard test view: send arbitrary key combinations and observe injected input live
- Structured key combo editor: up to 2 modifiers + 1 main key, replaces free-text input
- Templates view: create, edit, and delete reusable condition and action templates
- Trigger display names (custom labels shown in the mappings list)

### Changed
- Status view redesigned: gate toggle button prominently placed, variable table (A–Z) scrollable
- MIDI log view: added per-device filter dropdown

### Fixed
- Reconnect logic for USB MIDI devices unplugged and re-plugged during runtime
- SignalR hub disconnection not recovered after backend restart within the same session

---

## [0.3.0] – 2025-05-30

### Added
- `MidiController.Domain` — domain models (`MidiEvent`, `Trigger`, `Profile`), interfaces, enums
- `MidiController.Engine` — mapping engine, trigger executor, activation gate (variable `A`), delta tracking (`DeltaData2`)
- `MidiController.Infrastructure` — NAudio MIDI input (`MidiIn`), Win32 `SendInput` (P/Invoke), JSON config store (`%APPDATA%\MidiController\`)
- `MidiController.Host` — ASP.NET Core Minimal API, REST endpoints (`/api/v1/`), SignalR hubs (`/hubs/status`, `/hubs/midilog`)
- `MidiController.Frontend` — WPF application hosting the backend in-process
- State variables A–Z (−127…+127); reserved variables: `A` (gate), `X` (repeat), `Y` (hold ms), `Z` (pause ms)
- Lock-free event pipeline via `System.Threading.Channels` (capacity 512)
- Devices view: list and connect/disconnect physical MIDI input devices
- Mappings view: basic trigger list with create/delete
- Status view: activate/deactivate gate, live variable table
- MIDI log view: real-time stream of all incoming MIDI events
- Profile management: create, rename, delete, activate profiles stored as JSON

[Unreleased]: https://github.com/danroyton/midi-Midi2InputDevice/compare/v0.3.5...HEAD
[0.3.5]: https://github.com/danroyton/midi-Midi2InputDevice/compare/v0.3.4...v0.3.5
[0.3.4]: https://github.com/danroyton/midi-Midi2InputDevice/compare/v0.3.0...v0.3.4
[0.3.0]: https://github.com/danroyton/midi-Midi2InputDevice/compare/v0.2.0...v0.3.0
