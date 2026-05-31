# ADR-002: MidiMapper as a Windows Service

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2025-05-30 |
| Decision makers | Development team |

## Context

The backend can run either as a **regular desktop application** (tray app) or as a **Windows service**. Both variants have different implications for latency, auto-start, security, and the ability to inject keyboard input.

## Problem: SendInput and Windows Services

Windows services run in **Session 0** by default (isolated, non-interactive). `SendInput` and similar UI automation APIs only work in the **interactive user session** (Session 1+).

Possible approaches:

| Approach | Effort | Reliability |
|---|---|---|
| Service + tray helper process in Session 1 (via `CreateProcessAsUser`) | High | High |
| Service with `AllowInteractToDesktop` (deprecated, security risk) | Low | Low |
| No service; regular process with auto-start via Task Scheduler (interactive) | Low | Medium |
| Service injects into interactive session via named pipe to tray process | Medium | High |

## Options

### Option A: No service – auto-start via Task Scheduler
- The backend process starts when the user logs in.
- Runs in the user session → `SendInput` works directly.
- **Pro:** Simple, no Session 0 issue.
- **Con:** Cannot run without a logged-in user.
- **Con:** No automatic restart on crash without additional configuration.

### Option B: Windows service + named pipe to tray agent
- The service manages MIDI input, mapping engine, and API.
- A minimal tray agent in Session 1 receives injection commands via named pipe and calls `SendInput`.
- **Pro:** Auto-start, runs without login.
- **Pro:** Clean separation of service logic and UI injection.
- **Con:** Two processes, more complex communication.

### Option C: Windows service, direct injection into interactive session
- The service determines the active session via `WTSGetActiveConsoleSessionId`.
- Launches the injection helper via `CreateProcessAsUser`.
- **Pro:** Fully automatic, no manual tray agent.
- **Con:** Higher implementation effort, elevated permissions required.

## Decision

**Two-phase approach:**
1. **Phase 1:** Option A (no service, auto-start via Task Scheduler). Fast to implement, no session issues.
2. **Phase 2 (optional):** Option B – Windows service + tray agent, if running without a logged-in user is required.

The architecture is designed from the start so that injection logic is encapsulated behind an interface (`IInputInjector`) and can be replaced by a named-pipe proxy without changing the rest of the logic.

## Rationale

For the primary use case (gaming/productivity on a personal PC), running as a service without a logged-in user is not required. The additional effort of Option B is only worthwhile if that use case is explicitly requested.

## Consequences

- The `IInputInjector` interface must be defined.
- Phase 1 implementation: direct `SendInput` call within the backend process.
- `BackgroundService.ExecuteAsync` runs on a dedicated thread with `ThreadPriority.Highest`.
- A Task Scheduler task is created in the installer/setup script.
