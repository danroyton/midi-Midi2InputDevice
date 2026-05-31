# ADR-001: Choosing the MIDI Input Library

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2025-05-30 |
| Decision makers | Development team |

## Context

The backend must receive MIDI events from physical devices on Windows. The chosen library has a direct impact on latency and maintainability.

## Options

### Option A: NAudio (`NAudio.Midi.MidiIn`)
- **Pro:** Widely used, .NET-native, actively maintained, available via NuGet.
- **Pro:** Direct `winmm.dll` binding; low latency (~1–2 ms).
- **Con:** No low-level access to MIDI 2.0.
- **Con:** No multi-client support without virtual ports.

### Option B: RtMidi.NET
- **Pro:** Cross-platform (Windows/macOS/Linux).
- **Pro:** Slightly lower latency through RtMidi C++ backend.
- **Con:** Less widespread, smaller community.
- **Con:** Requires native DLL (deployment overhead).

### Option C: Windows MIDI Services (WinRT, Windows 11 24H2+)
- **Pro:** Official Microsoft SDK, MIDI 2.0, native multi-client.
- **Pro:** Lowest latency on supported hardware.
- **Con:** Windows 11 24H2 and newer only.
- **Con:** WinRT interop in a .NET 10 Worker Service is complex.

## Decision

**Option A (NAudio)** as the primary library, with an abstraction layer (`IMidiInputDevice`) that can later be swapped for Option B or C.

## Rationale

- NAudio is battle-tested, has a large community, and is directly available via NuGet.
- The ~1–2 ms latency is sufficient for the target application (keyboard input injection).
- The abstraction layer keeps a future migration to Windows MIDI Services possible once the Windows 11 minimum requirement becomes acceptable.

## Consequences

- The `IMidiInputDevice` interface must be defined.
- NAudio is added as a NuGet dependency (`NAudio`, `NAudio.WinMM`).
- MIDI 2.0 features are not available for now.
