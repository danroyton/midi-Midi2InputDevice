# ADR-003: Virtual MIDI Ports for Multi-Consumer Access

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2025-05-30 |
| Decision makers | Development team |

## Context

Windows MIDI drivers (WinMM) allow only **one simultaneous consumer** per physical MIDI input by default. When multiple applications (e.g. a DAW + MidiController) want to access the same device at the same time, the second `midiInOpen` call fails.

## Options

### Option A: loopMIDI (Tobias Erichsen)
- Free Windows driver for virtual MIDI loopback ports.
- Very widely used in the MIDI community.
- Can be accessed via COM automation (`LoopMIDI.COM`) or directly through the WinMM API.
- **Pro:** Stable, low latency, highly trusted in the community.
- **Con:** External dependency, must be installed separately.
- **Con:** No publicly documented .NET SDK.

### Option B: Windows MIDI Services (from Windows 11 24H2)
- Native multi-client support without a virtual driver.
- **Pro:** No external driver required.
- **Con:** Windows 11 24H2+ only; broad user base not reachable.

### Option C: Custom MIDI dispatch in the backend
- The backend opens the physical device exclusively and distributes events internally.
- Other applications receive events via virtual ports (created via loopMIDI or MIDI-Yoke).
- **Pro:** Maximum control; the backend is the sole consumer of the physical port.
- **Con:** When the backend is not running, other apps cannot access the device.

## Decision

**Option C** (custom dispatch) combined with **Option A** (loopMIDI for virtual output ports).

The backend opens the physical device exclusively. Events are forwarded internally and optionally mirrored to loopMIDI ports, allowing other applications to subscribe to the virtual ports.

## Rationale

- Option C is independent of loopMIDI for the core function; loopMIDI is only needed for optional mirroring.
- The backend as the sole consumer of the physical port enables maximum control and lowest latency.
- loopMIDI is the de facto standard for virtual MIDI ports on Windows; installation can be automated in the setup.

## Consequences

- The backend setup checks whether loopMIDI is installed; if not, a warning is displayed.
- An `IVirtualMidiPort` abstraction layer allows a later switch to Windows MIDI Services.
- Documentation must describe the loopMIDI installation.
- When the backend is not running, the physical port is **not** accessible to other applications. This behaviour must be explicitly communicated in the documentation.
